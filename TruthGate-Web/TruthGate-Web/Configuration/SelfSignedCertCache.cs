using System.Net;
using System.Security.Cryptography.X509Certificates;
using TruthGate_Web.Utils;

namespace TruthGate_Web.Configuration
{
    public sealed class SelfSignedCertCache
    {
        private static readonly TimeSpan RotationWindow = TimeSpan.FromDays(30);

        private readonly SemaphoreSlim _rotationLock = new(1, 1);
        private readonly TimeProvider _timeProvider;
        private readonly string _path;
        private X509Certificate2 _cached;

        public SelfSignedCertCache(X509Certificate2 initialCertificate)
            : this(
                initialCertificate,
                GetDefaultPersistencePath(),
                TimeProvider.System)
        {
        }

        public SelfSignedCertCache(
            X509Certificate2 initialCertificate,
            string persistencePath,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(initialCertificate);
            ArgumentException.ThrowIfNullOrWhiteSpace(persistencePath);

            _timeProvider = timeProvider ?? TimeProvider.System;
            _path = Path.GetFullPath(persistencePath);

            EnsureDirectory(Path.GetDirectoryName(_path)!);

            var persisted = TryLoadPersisted();
            if (persisted is not null && IsCurrentlyUsable(persisted))
            {
                _cached = persisted;
            }
            else
            {
                _cached = initialCertificate;
                PersistAtomicAsync(initialCertificate, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        private static string GetDefaultPersistencePath()
        {
            var certDir = Environment.GetEnvironmentVariable("TRUTHGATE_CERT_PATH");
            if (string.IsNullOrWhiteSpace(certDir))
                certDir = "/opt/truthgate/certs";

            return Path.Combine(
                Path.GetFullPath(certDir),
                "fallback-selfsigned.pfx");
        }

        public X509Certificate2 Get() => Volatile.Read(ref _cached);

        public async Task EnsureFreshAsync(CancellationToken ct = default)
        {
            var now = _timeProvider.GetUtcNow();
            var current = Get();

            if (IsCurrentlyUsable(current) &&
                new DateTimeOffset(current.NotAfter.ToUniversalTime()) - now > RotationWindow)
            {
                return;
            }

            await _rotationLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                now = _timeProvider.GetUtcNow();
                current = Get();

                if (IsCurrentlyUsable(current) &&
                    new DateTimeOffset(current.NotAfter.ToUniversalTime()) - now > RotationWindow)
                {
                    return;
                }

                var replacement = CreateReplacement();
                await PersistAtomicAsync(replacement, ct).ConfigureAwait(false);
                Volatile.Write(ref _cached, replacement);
            }
            finally
            {
                _rotationLock.Release();
            }
        }

        private bool IsCurrentlyUsable(X509Certificate2 certificate)
        {
            var now = _timeProvider.GetUtcNow();
            var notBefore = new DateTimeOffset(certificate.NotBefore.ToUniversalTime());
            var notAfter = new DateTimeOffset(certificate.NotAfter.ToUniversalTime());

            return certificate.HasPrivateKey && now >= notBefore && now < notAfter;
        }

        private X509Certificate2? TryLoadPersisted()
        {
            if (!File.Exists(_path))
                return null;

            try
            {
                HardenFile(_path);
                var bytes = File.ReadAllBytes(_path);
                return X509CertificateLoader.LoadPkcs12(bytes, ReadOnlySpan<char>.Empty);
            }
            catch
            {
                try
                {
                    var quarantine = $"{_path}.corrupt.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}";
                    File.Move(_path, quarantine);
                    HardenFile(quarantine);
                }
                catch
                {
                }

                return null;
            }
        }

        private static X509Certificate2 CreateReplacement()
        {
            IReadOnlyList<IPAddress> ips;
            var overrideValue = Environment.GetEnvironmentVariable("TRUTHGATE_CERT_IPS");

            if (!string.IsNullOrWhiteSpace(overrideValue))
            {
                ips = overrideValue
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(value => IPAddress.TryParse(value, out var ip) ? ip : null)
                    .Where(ip => ip is not null)
                    .Cast<IPAddress>()
                    .Distinct()
                    .ToList();
            }
            else
            {
                try
                {
                    ips = IPHelper.GetPublicInterfaceIPs()
                        .Distinct()
                        .ToList();
                }
                catch
                {
                    ips = Array.Empty<IPAddress>();
                }
            }

            if (ips.Count == 0)
                ips = new[] { IPAddress.Loopback, IPAddress.IPv6Loopback };

            return KestrelExtensions.CreateSelfSignedServerCert(
                dnsNames: Array.Empty<string>(),
                ipAddresses: ips);
        }

        private async Task PersistAtomicAsync(X509Certificate2 certificate, CancellationToken ct)
        {
            var directory = Path.GetDirectoryName(_path)!;
            EnsureDirectory(directory);

            var tempPath = Path.Combine(
                directory,
                $".{Path.GetFileName(_path)}.{Guid.NewGuid():N}.tmp");

            try
            {
                var bytes = certificate.Export(X509ContentType.Pkcs12);

                await using (var stream = new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 16 * 1024,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await stream.WriteAsync(bytes, ct).ConfigureAwait(false);
                    await stream.FlushAsync(ct).ConfigureAwait(false);
                    stream.Flush(flushToDisk: true);
                }

                HardenFile(tempPath);

                var validationBytes = await File.ReadAllBytesAsync(tempPath, ct).ConfigureAwait(false);
                using var validation = X509CertificateLoader.LoadPkcs12(
                    validationBytes,
                    ReadOnlySpan<char>.Empty);

                if (!validation.HasPrivateKey)
                    throw new InvalidOperationException("The persisted fallback certificate has no private key.");

                if (File.Exists(_path))
                {
                    try
                    {
                        File.Replace(tempPath, _path, destinationBackupFileName: null, ignoreMetadataErrors: true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Move(tempPath, _path, overwrite: true);
                    }
                    catch (IOException)
                    {
                        File.Move(tempPath, _path, overwrite: true);
                    }
                }
                else
                {
                    File.Move(tempPath, _path);
                }

                HardenFile(_path);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                }
            }
        }

        private static void EnsureDirectory(string directory)
        {
            Directory.CreateDirectory(directory);

            if (OperatingSystem.IsWindows())
                return;

            try
            {
                File.SetUnixFileMode(
                    directory,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite |
                    UnixFileMode.UserExecute);
            }
            catch
            {
            }
        }

        private static void HardenFile(string path)
        {
            if (OperatingSystem.IsWindows() || !File.Exists(path))
                return;

            try
            {
                File.SetUnixFileMode(
                    path,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite);
            }
            catch
            {
            }
        }
    }
}
