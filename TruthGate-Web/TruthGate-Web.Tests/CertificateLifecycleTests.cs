using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using TruthGate_Web.Configuration;
using TruthGate_Web.Models;
using TruthGate_Web.Services;
using Xunit;

namespace TruthGate_Web.Tests;

public sealed class CertificateLifecycleTests : IDisposable
{
    private readonly string _tempDirectory =
        Path.Combine(Path.GetTempPath(), $"truthgate-tls-tests-{Guid.NewGuid():N}");

    public CertificateLifecycleTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task RenewalDueCertificate_RemainsServedWhileRenewalRuns()
    {
        const string host = "truthgate.io";
        using var existing = CreateCertificate(
            host,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(10));

        var store = new MemoryCertificateStore();
        store.Set(host, existing);

        var issuerEntered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseIssuer = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var issuer = new DelegateIssuer(async (_, ct) =>
        {
            issuerEntered.TrySetResult(true);
            await releaseIssuer.Task.WaitAsync(ct);
            return CreateCertificate(
                host,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddDays(90));
        });

        using var fallback = CreateCertificate(
            "localhost",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(365));

        var provider = CreateProvider(host, store, issuer, fallback);
        var reconciliation = provider.ReconcileAsync(host);

        await issuerEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(existing, provider.TryLoadIssued(host));
        Assert.Equal(1, issuer.CallCount);

        releaseIssuer.TrySetResult(true);
        await reconciliation;
    }

    [Fact]
    public async Task ExpiredCertificate_IsAutomaticallyReplaced()
    {
        const string host = "truthgate.io";
        using var expired = CreateCertificate(
            host,
            DateTimeOffset.UtcNow.AddDays(-90),
            DateTimeOffset.UtcNow.AddDays(-1));

        var store = new MemoryCertificateStore();
        store.Set(host, expired);

        var issuer = new DelegateIssuer((_, _) =>
            Task.FromResult<X509Certificate2?>(
                CreateCertificate(
                    host,
                    DateTimeOffset.UtcNow.AddMinutes(-5),
                    DateTimeOffset.UtcNow.AddDays(90))));

        using var fallback = CreateCertificate(
            "localhost",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(365));

        var provider = CreateProvider(host, store, issuer, fallback);

        await provider.ReconcileAsync(host);

        var served = provider.TryLoadIssued(host);
        Assert.NotNull(served);
        Assert.True(served.NotAfter.ToUniversalTime() > DateTime.UtcNow.AddDays(80));
        Assert.Equal(1, issuer.CallCount);
    }

    [Fact]
    public async Task ConcurrentReconciliation_ProducesOneAcmeOrder()
    {
        const string host = "truthgate.io";
        var store = new MemoryCertificateStore();

        var issuerEntered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseIssuer = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var issuer = new DelegateIssuer(async (_, ct) =>
        {
            issuerEntered.TrySetResult(true);
            await releaseIssuer.Task.WaitAsync(ct);
            return CreateCertificate(
                host,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddDays(90));
        });

        using var fallback = CreateCertificate(
            "localhost",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(365));

        var provider = CreateProvider(host, store, issuer, fallback);

        var calls = Enumerable.Range(0, 100)
            .Select(_ => provider.ReconcileAsync(host))
            .ToArray();

        await issuerEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, issuer.CallCount);

        releaseIssuer.TrySetResult(true);
        await Task.WhenAll(calls);

        Assert.Equal(1, issuer.CallCount);
    }

    [Fact]
    public async Task CorruptStoredCertificate_IsQuarantinedAndRepaired()
    {
        const string host = "truthgate.io";
        var store = new InitiallyCorruptCertificateStore();

        var issuer = new DelegateIssuer((_, _) =>
            Task.FromResult<X509Certificate2?>(
                CreateCertificate(
                    host,
                    DateTimeOffset.UtcNow.AddMinutes(-5),
                    DateTimeOffset.UtcNow.AddDays(90))));

        using var fallback = CreateCertificate(
            "localhost",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(365));

        var provider = CreateProvider(host, store, issuer, fallback);

        await provider.ReconcileAsync(host);

        Assert.True(store.Quarantined);
        Assert.NotNull(provider.TryLoadIssued(host));
        Assert.Equal(1, issuer.CallCount);
    }

    [Fact]
    public void WrongHostnameCertificate_IsRejected()
    {
        using var certificate = CreateCertificate(
            "other.example",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(90));

        var inspection = CertificateInspector.Inspect(
            certificate,
            "truthgate.io",
            DateTimeOffset.UtcNow,
            TimeSpan.FromDays(30));

        Assert.Equal(CertificateState.WrongHostname, inspection.State);
        Assert.False(inspection.CanServe);
        Assert.True(inspection.NeedsIssuance);
    }

    [Fact]
    public async Task FileStore_PersistsReloadableCertificateAtomically()
    {
        const string host = "truthgate.io";
        using var certificate = CreateCertificate(
            host,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(90));

        var store = new FileCertStore(_tempDirectory);
        await store.SaveAsync(host, certificate, CancellationToken.None);

        using var loaded = await store.LoadAsync(host, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.True(loaded.HasPrivateKey);
        Assert.True(loaded.MatchesHostname(
            host,
            allowWildcards: true,
            allowCommonName: false));

        var temporaryFiles = Directory
            .EnumerateFiles(_tempDirectory, "*.tmp", SearchOption.TopDirectoryOnly)
            .ToArray();

        Assert.Empty(temporaryFiles);
    }

    private LiveCertProvider CreateProvider(
        string host,
        ICertificateStore store,
        IAcmeIssuer issuer,
        X509Certificate2 fallback)
    {
        var fallbackPath = Path.Combine(
            _tempDirectory,
            $"{Guid.NewGuid():N}-fallback.pfx");

        var fallbackCache = new SelfSignedCertCache(fallback, fallbackPath);

        return new LiveCertProvider(
            fallbackCache,
            store,
            issuer,
            new TestConfigService(host),
            NullLogger<LiveCertProvider>.Instance);
    }

    private static X509Certificate2 CreateCertificate(
        string host,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={host}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName(host);
        request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature |
                X509KeyUsageFlags.KeyEncipherment,
                critical: false));

        var eku = new OidCollection
        {
            new("1.3.6.1.5.5.7.3.1")
        };
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(eku, critical: false));

        using var generated = request.CreateSelfSigned(notBefore, notAfter);
        var pfx = generated.Export(X509ContentType.Pkcs12);
        return X509CertificateLoader.LoadPkcs12(
            pfx,
            ReadOnlySpan<char>.Empty);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
        }
    }

    private sealed class TestConfigService : IConfigService
    {
        private readonly Config _config;

        public TestConfigService(string host)
        {
            _config = new Config
            {
                Domains =
                [
                    new EdgeDomain
                    {
                        Domain = host,
                        UseSSL = "true"
                    }
                ]
            };
        }

        public Config Get() => _config;
        public Task SaveAsync(Config newConfig, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task UpdateAsync(Action<Config> mutator, CancellationToken ct = default)
            => Task.CompletedTask;
        public string ConfigPath => string.Empty;
    }

    private sealed class DelegateIssuer : IAcmeIssuer
    {
        private readonly Func<string, CancellationToken, Task<X509Certificate2?>> _handler;
        private int _callCount;

        public DelegateIssuer(
            Func<string, CancellationToken, Task<X509Certificate2?>> handler)
        {
            _handler = handler;
        }

        public int CallCount => Volatile.Read(ref _callCount);

        public Task<X509Certificate2?> IssueOrRenewAsync(
            string host,
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref _callCount);
            return _handler(host, ct);
        }
    }

    private sealed class MemoryCertificateStore : ICertificateStore
    {
        private readonly ConcurrentDictionary<string, X509Certificate2> _certificates =
            new(StringComparer.OrdinalIgnoreCase);

        public void Set(string host, X509Certificate2 certificate)
            => _certificates[host] = certificate;

        public Task<X509Certificate2?> LoadAsync(string host, CancellationToken ct)
        {
            _certificates.TryGetValue(host, out var certificate);
            return Task.FromResult(certificate);
        }

        public Task SaveAsync(
            string host,
            X509Certificate2 certificate,
            CancellationToken ct)
        {
            _certificates[host] = certificate;
            return Task.CompletedTask;
        }
    }

    private sealed class InitiallyCorruptCertificateStore :
        ICertificateStore,
        ICertificateStoreMaintenance
    {
        private X509Certificate2? _certificate;
        private bool _throwOnLoad = true;

        public bool Quarantined { get; private set; }

        public Task<X509Certificate2?> LoadAsync(string host, CancellationToken ct)
        {
            if (_throwOnLoad)
                throw new CryptographicException("The PFX is corrupt.");

            return Task.FromResult(_certificate);
        }

        public Task SaveAsync(
            string host,
            X509Certificate2 certificate,
            CancellationToken ct)
        {
            _certificate = certificate;
            _throwOnLoad = false;
            return Task.CompletedTask;
        }

        public Task QuarantineAsync(
            string host,
            string reason,
            CancellationToken ct)
        {
            Quarantined = true;
            _throwOnLoad = false;
            return Task.CompletedTask;
        }

        public void HardenExistingPermissions()
        {
        }
    }
}
