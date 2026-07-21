using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using TruthGate_Web.Services;

namespace TruthGate_Web.Configuration
{
    public enum SslDecisionKind
    {
        SelfSigned,
        NoneFailTls,
        RealIfPresent
    }

    public interface IAcmeIssuerLabel
    {
        string Label { get; }
    }

    public readonly record struct SslDecision(SslDecisionKind Kind);

    public sealed record CertificateLifecycleStatus(
        string Host,
        CertificateState State,
        DateTimeOffset? NotBefore,
        DateTimeOffset? NotAfter,
        bool CurrentlyServable,
        bool IssuanceInFlight,
        int FailureCount,
        DateTimeOffset? NextRetry,
        DateTimeOffset? LastSuccessfulIssuance,
        string? LastError);

    public sealed class LiveCertProvider
    {
        private static readonly TimeSpan RenewalWindow = TimeSpan.FromDays(30);

        private readonly SemaphoreSlim _throttle = new(2);
        private readonly ConcurrentDictionary<string, Lazy<Task>> _inflight =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, RetryState> _cooldown =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, X509Certificate2> _issuedCache =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, CertificateLifecycleStatus> _status =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly SelfSignedCertCache _self;
        private readonly ICertificateStore _store;
        private readonly IAcmeIssuer _acme;
        private readonly IConfigService _config;
        private readonly ILogger<LiveCertProvider>? _log;
        private readonly string _acmeLabel;
        private readonly TimeProvider _timeProvider;

        private readonly record struct RetryState(
            DateTimeOffset Until,
            int Failures,
            string LastError);

        public LiveCertProvider(
            SelfSignedCertCache self,
            ICertificateStore store,
            IAcmeIssuer acme,
            IConfigService config,
            ILogger<LiveCertProvider>? log = null,
            TimeProvider? timeProvider = null)
        {
            _self = self;
            _store = store;
            _acme = acme;
            _config = config;
            _log = log;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _acmeLabel = (acme as IAcmeIssuerLabel)?.Label ?? "unknown";
        }

        public X509Certificate2 GetSelfSigned() => _self.Get();

        public X509Certificate2? TryLoadIssued(string exactHostKey)
        {
            var key = CertificateInspector.NormalizeHost(exactHostKey);
            if (key.Length == 0)
                return null;

            if (!_issuedCache.TryGetValue(key, out var certificate))
                return null;

            var inspection = CertificateInspector.Inspect(
                certificate,
                key,
                _timeProvider.GetUtcNow(),
                RenewalWindow);

            if (inspection.CanServe)
            {
                UpdateStatus(key, inspection, currentlyServable: true, lastError: null);
                return certificate;
            }

            _issuedCache.TryRemove(key, out _);
            UpdateStatus(key, inspection, currentlyServable: false, lastError: inspection.Detail);
            return null;
        }

        public async Task WarmCacheAsync(string exactHostKey, CancellationToken ct = default)
        {
            var key = CertificateInspector.NormalizeHost(exactHostKey);
            if (key.Length == 0)
                return;

            var (_, inspection) = await LoadAndInspectAsync(key, quarantineCorrupt: true, ct)
                .ConfigureAwait(false);

            UpdateStatus(
                key,
                inspection,
                currentlyServable: inspection.CanServe,
                lastError: inspection.CanServe ? null : inspection.Detail);
        }

        public Task ReconcileAsync(string exactHostKey, CancellationToken ct = default)
        {
            var (task, _) = GetOrStartReconciliation(exactHostKey, ct);
            return task;
        }

        public bool TryQueueIssueIfMissing(string exactHostKey)
        {
            var (_, started) = GetOrStartReconciliation(exactHostKey, CancellationToken.None);
            return started;
        }

        public void QueueIssueIfMissing(string host) => TryQueueIssueIfMissing(host);

        public bool IsInFlight(string host)
        {
            var key = CertificateInspector.NormalizeHost(host);
            return key.Length != 0 && _inflight.ContainsKey(key);
        }

        public IReadOnlyCollection<CertificateLifecycleStatus> GetStatusSnapshot()
            => _status.Values
                .OrderBy(status => status.Host, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        public object GetCooldownSnapshot(string host)
        {
            var key = CertificateInspector.NormalizeHost(host);
            if (_cooldown.TryGetValue(key, out var retry))
            {
                return new
                {
                    host = key,
                    coolingDown = _timeProvider.GetUtcNow() < retry.Until,
                    until = retry.Until,
                    failures = retry.Failures,
                    lastError = retry.LastError
                };
            }

            return new { host = key, coolingDown = false, failures = 0 };
        }

        private (Task Task, bool Started) GetOrStartReconciliation(
            string exactHostKey,
            CancellationToken ct)
        {
            var key = CertificateInspector.NormalizeHost(exactHostKey);
            if (key.Length == 0)
                return (Task.CompletedTask, false);

            var decision = DecideForHostIncludingStarish(key);
            if (decision.Kind != SslDecisionKind.RealIfPresent)
                return (Task.CompletedTask, false);

            if (_cooldown.TryGetValue(key, out var retry) &&
                _timeProvider.GetUtcNow() < retry.Until)
            {
                _log?.LogWarning(
                    "[TLS] {Host} is in cooldown until {Until} after {Failures} failure(s); reconciliation skipped",
                    key,
                    retry.Until,
                    retry.Failures);

                RefreshInFlightStatus(key, false);
                return (Task.CompletedTask, false);
            }

            var candidate = new Lazy<Task>(
                () => ReconcileCoreAsync(key, ct),
                LazyThreadSafetyMode.ExecutionAndPublication);

            var winner = _inflight.GetOrAdd(key, candidate);
            var started = ReferenceEquals(candidate, winner);
            var task = winner.Value;

            if (started)
                _ = CleanupFlightAsync(key, candidate, task);

            RefreshInFlightStatus(key, true);
            return (task, started);
        }

        private async Task CleanupFlightAsync(string key, Lazy<Task> owner, Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            finally
            {
                if (_inflight.TryGetValue(key, out var current) &&
                    ReferenceEquals(current, owner))
                {
                    _inflight.TryRemove(key, out _);
                }

                RefreshInFlightStatus(key, false);
            }
        }

        private async Task ReconcileCoreAsync(string key, CancellationToken ct)
        {
            X509Certificate2? lastKnownServable = null;

            try
            {
                var (existing, inspection) = await LoadAndInspectAsync(
                    key,
                    quarantineCorrupt: true,
                    ct).ConfigureAwait(false);

                if (inspection.CanServe)
                    lastKnownServable = existing;

                UpdateStatus(
                    key,
                    inspection,
                    currentlyServable: inspection.CanServe,
                    lastError: inspection.CanServe ? null : inspection.Detail);

                if (!inspection.NeedsIssuance)
                {
                    _cooldown.TryRemove(key, out _);
                    return;
                }

                await _throttle.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    var (latest, latestInspection) = await LoadAndInspectAsync(
                        key,
                        quarantineCorrupt: true,
                        ct).ConfigureAwait(false);

                    if (latestInspection.CanServe)
                        lastKnownServable = latest;

                    UpdateStatus(
                        key,
                        latestInspection,
                        currentlyServable: latestInspection.CanServe,
                        lastError: latestInspection.CanServe ? null : latestInspection.Detail);

                    if (!latestInspection.NeedsIssuance)
                    {
                        _cooldown.TryRemove(key, out _);
                        return;
                    }

                    _log?.LogInformation(
                        "[TLS] [{Label}] {State} certificate for {Host}; issuing/renewing",
                        _acmeLabel,
                        latestInspection.State,
                        key);

                    var issued = await _acme.IssueOrRenewAsync(key, ct).ConfigureAwait(false);
                    if (issued is null)
                        throw new InvalidOperationException(
                            $"ACME issuer returned no certificate for '{key}'.");

                    var issuedInspection = CertificateInspector.Inspect(
                        issued,
                        key,
                        _timeProvider.GetUtcNow(),
                        RenewalWindow);

                    if (!issuedInspection.CanServe)
                    {
                        throw new InvalidOperationException(
                            $"ACME returned an unusable certificate for '{key}': " +
                            $"{issuedInspection.State}. {issuedInspection.Detail}");
                    }

                    await _store.SaveAsync(key, issued, ct).ConfigureAwait(false);

                    var (persisted, persistedInspection) = await LoadAndInspectAsync(
                        key,
                        quarantineCorrupt: false,
                        ct).ConfigureAwait(false);

                    if (persisted is null || !persistedInspection.CanServe)
                    {
                        throw new InvalidOperationException(
                            $"The persisted replacement certificate for '{key}' failed validation: " +
                            $"{persistedInspection.State}. {persistedInspection.Detail}");
                    }

                    _issuedCache[key] = persisted;
                    _cooldown.TryRemove(key, out _);

                    var now = _timeProvider.GetUtcNow();
                    _status[key] = new CertificateLifecycleStatus(
                        key,
                        persistedInspection.State,
                        persistedInspection.NotBefore,
                        persistedInspection.NotAfter,
                        CurrentlyServable: true,
                        IssuanceInFlight: true,
                        FailureCount: 0,
                        NextRetry: null,
                        LastSuccessfulIssuance: now,
                        LastError: null);

                    _log?.LogInformation(
                        "[TLS] [{Label}] installed certificate for {Host}, valid until {NotAfter}",
                        _acmeLabel,
                        key,
                        persistedInspection.NotAfter);
                }
                finally
                {
                    _throttle.Release();
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (lastKnownServable is not null)
                    _issuedCache[key] = lastKnownServable;
                else
                    _issuedCache.TryRemove(key, out _);

                _log?.LogError(
                    ex,
                    "[TLS] [{Label}] reconciliation failed for {Host}",
                    _acmeLabel,
                    key);

                RegisterFailure(key, ex.Message);
            }
        }

        private async Task<(X509Certificate2? Certificate, CertificateInspection Inspection)>
            LoadAndInspectAsync(
                string key,
                bool quarantineCorrupt,
                CancellationToken ct)
        {
            try
            {
                var certificate = await _store.LoadAsync(key, ct).ConfigureAwait(false);
                var inspection = CertificateInspector.Inspect(
                    certificate,
                    key,
                    _timeProvider.GetUtcNow(),
                    RenewalWindow);

                if (inspection.CanServe && certificate is not null)
                    _issuedCache[key] = certificate;
                else
                    _issuedCache.TryRemove(key, out _);

                return (certificate, inspection);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _issuedCache.TryRemove(key, out _);

                _log?.LogWarning(
                    ex,
                    "[TLS] Stored certificate for {Host} could not be loaded and will be repaired",
                    key);

                if (quarantineCorrupt && _store is ICertificateStoreMaintenance maintenance)
                {
                    try
                    {
                        await maintenance.QuarantineAsync(key, ex.Message, ct)
                            .ConfigureAwait(false);
                    }
                    catch (Exception quarantineException)
                    {
                        _log?.LogWarning(
                            quarantineException,
                            "[TLS] Could not quarantine the broken certificate for {Host}",
                            key);
                    }
                }

                return (
                    null,
                    new CertificateInspection(
                        CertificateState.Corrupt,
                        null,
                        null,
                        ex.Message));
            }
        }

        private void RegisterFailure(string key, string error)
        {
            var now = _timeProvider.GetUtcNow();
            var retry = _cooldown.AddOrUpdate(
                key,
                _ => new RetryState(now.AddMinutes(1), 1, error),
                (_, previous) =>
                {
                    var failures = Math.Min(previous.Failures + 1, 5);
                    var delay = failures switch
                    {
                        1 => TimeSpan.FromMinutes(1),
                        2 => TimeSpan.FromMinutes(5),
                        3 => TimeSpan.FromMinutes(15),
                        4 => TimeSpan.FromMinutes(30),
                        _ => TimeSpan.FromHours(1)
                    };

                    return new RetryState(now + delay, failures, error);
                });

            var existing = _status.TryGetValue(key, out var status)
                ? status
                : new CertificateLifecycleStatus(
                    key,
                    CertificateState.Missing,
                    null,
                    null,
                    CurrentlyServable: false,
                    IssuanceInFlight: true,
                    FailureCount: 0,
                    NextRetry: null,
                    LastSuccessfulIssuance: null,
                    LastError: null);

            _status[key] = existing with
            {
                IssuanceInFlight = true,
                FailureCount = retry.Failures,
                NextRetry = retry.Until,
                LastError = error
            };

            _log?.LogWarning(
                "[TLS] {Host} retry scheduled for {Until} after {Failures} failure(s)",
                key,
                retry.Until,
                retry.Failures);
        }

        private void UpdateStatus(
            string key,
            CertificateInspection inspection,
            bool currentlyServable,
            string? lastError)
        {
            var retry = _cooldown.TryGetValue(key, out var existingRetry)
                ? existingRetry
                : (RetryState?)null;

            var previous = _status.TryGetValue(key, out var existing)
                ? existing
                : null;

            _status[key] = new CertificateLifecycleStatus(
                key,
                inspection.State,
                inspection.NotBefore,
                inspection.NotAfter,
                currentlyServable,
                IsInFlight(key),
                retry?.Failures ?? 0,
                retry?.Until,
                previous?.LastSuccessfulIssuance,
                lastError ?? retry?.LastError);
        }

        private void RefreshInFlightStatus(string key, bool inFlight)
        {
            if (!_status.TryGetValue(key, out var existing))
                return;

            _status[key] = existing with { IssuanceInFlight = inFlight };
        }

        private (string? BaseHost, bool Enabled) GetIpnsWildcardBase()
        {
            var wildcard = _config.Get().IpnsWildCardSubDomain;
            if (wildcard is null)
                return (null, false);

            var baseHost = CertificateInspector.NormalizeHost(wildcard.WildCardSubDomain ?? string.Empty);
            var useSsl = bool.TryParse(wildcard.UseSSL, out var enabled) && enabled;

            if (baseHost.Length == 0 || !useSsl)
                return (null, false);

            return (baseHost, true);
        }

        private static string? LeftLabel(string host)
        {
            var normalized = CertificateInspector.NormalizeHost(host);
            var index = normalized.IndexOf('.');
            return index <= 0 ? null : normalized[..index];
        }

        private bool IsAuthorizedIpnsStarishHost(string host)
        {
            host = CertificateInspector.NormalizeHost(host);
            var (baseHost, enabled) = GetIpnsWildcardBase();
            if (!enabled || baseHost is null)
                return false;

            if (!(host == baseHost ||
                  host.EndsWith("." + baseHost, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var left = LeftLabel(host);
            if (left is null)
                return false;

            var authorized = _config.Get().Domains
                .Select(domain => new { domain.IpnsPeerId, domain.IpnsKeyName })
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value.IpnsPeerId) ||
                    !string.IsNullOrWhiteSpace(value.IpnsKeyName));

            foreach (var value in authorized)
            {
                if (!string.IsNullOrWhiteSpace(value.IpnsPeerId) &&
                    string.Equals(
                        left,
                        value.IpnsPeerId.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(value.IpnsKeyName) &&
                    string.Equals(
                        left,
                        value.IpnsKeyName.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public SslDecision DecideForHostIncludingStarish(string host)
        {
            host = CertificateInspector.NormalizeHost(host);

            var match = _config.Get().Domains.FirstOrDefault(domain =>
                string.Equals(
                    CertificateInspector.NormalizeHost(domain.Domain ?? string.Empty),
                    host,
                    StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                var useSsl = bool.TryParse(match.UseSSL, out var enabled) && enabled;
                return useSsl
                    ? new SslDecision(SslDecisionKind.RealIfPresent)
                    : new SslDecision(SslDecisionKind.NoneFailTls);
            }

            if (IsAuthorizedIpnsStarishHost(host))
                return new SslDecision(SslDecisionKind.RealIfPresent);

            return new SslDecision(SslDecisionKind.SelfSigned);
        }

        public SslDecision DecideForHost(string host)
        {
            host = CertificateInspector.NormalizeHost(host);

            var match = _config.Get().Domains.FirstOrDefault(domain =>
                string.Equals(
                    CertificateInspector.NormalizeHost(domain.Domain ?? string.Empty),
                    host,
                    StringComparison.OrdinalIgnoreCase));

            if (match is null)
                return new SslDecision(SslDecisionKind.SelfSigned);

            var useSsl = bool.TryParse(match.UseSSL, out var enabled) && enabled;
            return useSsl
                ? new SslDecision(SslDecisionKind.RealIfPresent)
                : new SslDecision(SslDecisionKind.NoneFailTls);
        }

        public IEnumerable<string> EnumerateAuthorizedIpnsHosts()
        {
            var (baseHost, enabled) = GetIpnsWildcardBase();
            if (!enabled || baseHost is null)
                yield break;

            var config = _config.Get();
            foreach (var domain in config.Domains.Where(domain =>
                         bool.TryParse(domain.UseSSL, out var useSsl) && useSsl))
            {
                var identifiers = new[]
                    {
                        domain.IpnsPeerId?.Trim(),
                        domain.IpnsKeyName?.Trim()
                    }
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var identifier in identifiers)
                    yield return $"{identifier!.ToLowerInvariant()}.{baseHost}";
            }
        }
    }
}
