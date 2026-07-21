using TruthGate_Web.Services;

namespace TruthGate_Web.Configuration
{
    public sealed class EagerIssueAtStartup : IHostedService
    {
        private readonly IConfigService _config;
        private readonly LiveCertProvider _live;
        private readonly ILogger<EagerIssueAtStartup> _logger;

        public EagerIssueAtStartup(
            IConfigService config,
            LiveCertProvider live,
            ILogger<EagerIssueAtStartup> logger)
        {
            _config = config;
            _live = live;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            var config = _config.Get();

            var hosts = config.Domains
                .Where(domain => bool.TryParse(domain.UseSSL, out var enabled) && enabled)
                .Select(domain => CertificateInspector.NormalizeHost(domain.Domain ?? string.Empty))
                .Where(host => host.Length != 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var host in _live.EnumerateAuthorizedIpnsHosts())
                hosts.Add(host);

            try
            {
                await Task.WhenAll(
                    hosts.Select(host => _live.WarmCacheAsync(host, ct)))
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TLS] Failed to hydrate the certificate cache at startup");
            }
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
