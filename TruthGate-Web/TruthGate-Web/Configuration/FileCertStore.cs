using System.Security.Cryptography.X509Certificates;

namespace TruthGate_Web.Configuration
{
    public interface ICertificateStore
    {
        Task<X509Certificate2?> LoadAsync(string host, CancellationToken ct);
        Task SaveAsync(string host, X509Certificate2 cert, CancellationToken ct);
    }

    public interface ICertificateStoreMaintenance
    {
        Task QuarantineAsync(string host, string reason, CancellationToken ct);
        void HardenExistingPermissions();
    }

    public sealed class FileCertStore : ICertificateStore, ICertificateStoreMaintenance
    {
        private readonly string _dir;
        private readonly bool _staging;

        public FileCertStore(string dir, bool staging = false)
        {
            _dir = dir;
            _staging = staging;
            HardenExistingPermissions();
        }

        private static string SafeFileNameForKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) key = "unknown";

            var safe = key.Trim()
                .Replace("*.", "_wildcard_.")
                .Replace(":", "_")
                .Replace("/", "_")
                .Replace("\\", "_");

            foreach (var c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');

            return safe.ToLowerInvariant();
        }

        private string PathFor(string hostOrKey)
        {
            var safe = SafeFileNameForKey(hostOrKey);
            return Path.Combine(_dir, $"{safe}{(_staging ? ".staging" : "")}.pfx");
        }

        public async Task<X509Certificate2?> LoadAsync(string host, CancellationToken ct)
        {
            EnsureDirectory();

            var path = PathFor(host);
            if (!File.Exists(path))
            {
                if (_staging)
                    return null;

                var legacy = Path.Combine(_dir, $"{SafeFileNameForKey(host)}.pfx");
                if (!File.Exists(legacy))
                    return null;

                path = legacy;
            }

            HardenFile(path);
            var bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
            return X509CertificateLoader.LoadPkcs12(bytes, ReadOnlySpan<char>.Empty);
        }

        public async Task SaveAsync(string host, X509Certificate2 cert, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(cert);
            if (!cert.HasPrivateKey)
                throw new InvalidOperationException($"Refusing to save certificate for '{host}' because it has no private key.");

            EnsureDirectory();

            var path = PathFor(host);
            var tempPath = Path.Combine(
                _dir,
                $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

            try
            {
                var bytes = cert.Export(X509ContentType.Pkcs12);

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

                var persistedBytes = await File.ReadAllBytesAsync(tempPath, ct).ConfigureAwait(false);
                using (var validation = X509CertificateLoader.LoadPkcs12(
                    persistedBytes,
                    ReadOnlySpan<char>.Empty))
                {
                    if (!validation.HasPrivateKey)
                        throw new InvalidOperationException(
                            $"Persisted certificate for '{host}' lost its private key.");
                }

                ReplaceAtomically(tempPath, path);
                HardenFile(path);
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        public Task QuarantineAsync(string host, string reason, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureDirectory();

            var path = PathFor(host);
            if (!File.Exists(path))
                return Task.CompletedTask;

            var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
            var quarantinePath = $"{path}.corrupt.{stamp}";
            var suffix = 0;

            while (File.Exists(quarantinePath))
                quarantinePath = $"{path}.corrupt.{stamp}.{++suffix}";

            File.Move(path, quarantinePath);
            HardenFile(quarantinePath);

            return Task.CompletedTask;
        }

        public void HardenExistingPermissions()
        {
            EnsureDirectory();

            foreach (var path in Directory.EnumerateFiles(_dir, "*.pfx", SearchOption.TopDirectoryOnly))
                HardenFile(path);

            foreach (var path in Directory.EnumerateFiles(_dir, "account*.pem", SearchOption.TopDirectoryOnly))
                HardenFile(path);
        }

        private void EnsureDirectory()
        {
            Directory.CreateDirectory(_dir);

            if (OperatingSystem.IsWindows())
                return;

            try
            {
                File.SetUnixFileMode(
                    _dir,
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

        private static void ReplaceAtomically(string tempPath, string destinationPath)
        {
            if (!File.Exists(destinationPath))
            {
                File.Move(tempPath, destinationPath);
                return;
            }

            try
            {
                File.Replace(tempPath, destinationPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            catch (PlatformNotSupportedException)
            {
                File.Move(tempPath, destinationPath, overwrite: true);
            }
            catch (IOException)
            {
                File.Move(tempPath, destinationPath, overwrite: true);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
