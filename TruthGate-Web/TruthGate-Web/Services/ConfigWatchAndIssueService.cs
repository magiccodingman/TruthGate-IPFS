using TruthGate_Web.Configuration;

namespace TruthGate_Web.Services
{
    public sealed class ConfigWatchAndIssueService : BackgroundService
    {
        private static readonly TimeSpan ReconcileInterval = TimeSpan.FromMinutes(2);

        private readonly IConfigService _config;
        private readonly LiveCertProvider _live;
        private readonly SelfSignedCertCache _fallback;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<ConfigWatchAndIssueService> _logger;

        public ConfigWatchAndIssueService(
            IConfigService config,
            LiveCertProvider live,
            SelfSignedCertCache fallback,
            IHostApplicationLifetime lifetime,
            ILogger<ConfigWatchAndIssueService> logger)
        {
            _config = config;
            _live = live;
            _fallback = fallback;
            _lifetime = lifetime;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await WaitForApplicationStartedAsync(stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _fallback.EnsureFreshAsync(stoppingToken).ConfigureAwait(false);

                    var hosts = GetDesiredHosts();
                    await Task.WhenAll(
                        hosts.Select(host => _live.ReconcileAsync(host, stoppingToken)))
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TLS] Certificate reconciliation cycle failed");
                }

                try
                {
                    await Task.Delay(ReconcileInterval, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private HashSet<string> GetDesiredHosts()
        {
            var config = _config.Get();

            var hosts = config.Domains
                .Where(domain => bool.TryParse(domain.UseSSL, out var enabled) && enabled)
                .Select(domain => CertificateInspector.NormalizeHost(domain.Domain ?? string.Empty))
                .Where(host => host.Length != 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var host in _live.EnumerateAuthorizedIpnsHosts())
                hosts.Add(host);

            return hosts;
        }

        private async Task WaitForApplicationStartedAsync(CancellationToken stoppingToken)
        {
            if (_lifetime.ApplicationStarted.IsCancellationRequested)
                return;

            var started = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            using var startedRegistration = _lifetime.ApplicationStarted.Register(
                () => started.TrySetResult(true));

            using var stoppingRegistration = stoppingToken.Register(
                () => started.TrySetCanceled(stoppingToken));

            await started.Task.ConfigureAwait(false);
        }
    }
}
