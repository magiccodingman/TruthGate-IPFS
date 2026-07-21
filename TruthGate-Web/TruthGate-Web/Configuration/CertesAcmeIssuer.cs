using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;

namespace TruthGate_Web.Configuration
{
    public sealed class CertesAcmeIssuer : IAcmeIssuer, IAcmeIssuerLabel
    {
        private readonly Uri _dirUri;
        private readonly string _accountPemPath;
        private readonly IAcmeChallengeStore _challengeStore;
        private readonly ILogger<CertesAcmeIssuer> _logger;
        private readonly bool _isStaging;

        public string Label => _isStaging ? "staging" : "prod";

        public CertesAcmeIssuer(
            IAcmeChallengeStore challengeStore,
            ILogger<CertesAcmeIssuer> logger,
            bool useStaging = false,
            string accountPemPath = "/opt/truthgate/certs/account.pem")
        {
            _challengeStore = challengeStore;
            _accountPemPath = accountPemPath;
            _logger = logger;
            _isStaging = useStaging;
            _dirUri = useStaging
                ? WellKnownServers.LetsEncryptStagingV2
                : WellKnownServers.LetsEncryptV2;
        }

        public async Task<X509Certificate2?> IssueOrRenewAsync(
            string host,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("ACME[{Dir}] start {Host}", Label, host);

                var accountKey = await LoadOrCreateAccountKeyAsync(ct).ConfigureAwait(false);
                var acme = new AcmeContext(_dirUri, accountKey);

                try
                {
                    await acme.NewAccount(Array.Empty<string>(), true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "ACME[{Dir}] account already registered or reusable", Label);
                }

                var order = await acme.NewOrder(new[] { host }).ConfigureAwait(false);
                _logger.LogInformation("ACME[{Dir}] order created for {Host}", Label, host);

                var authorizations = await order.Authorizations().ConfigureAwait(false);
                foreach (var authorization in authorizations)
                {
                    var http = await authorization.Http().ConfigureAwait(false);
                    var token = http.Token;
                    var keyAuthorization = http.KeyAuthz;
                    var url = $"http://{host}/.well-known/acme-challenge/{token}";

                    _challengeStore.Put(token, keyAuthorization, TimeSpan.FromMinutes(10));
                    try
                    {
                        await RunPreflightAsync(url, keyAuthorization, ct).ConfigureAwait(false);
                        await http.Validate().ConfigureAwait(false);

                        var challengeDeadline =
                            DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);

                        while (true)
                        {
                            ct.ThrowIfCancellationRequested();

                            var challenge = await http.Resource().ConfigureAwait(false);
                            if (challenge.Status == ChallengeStatus.Valid)
                            {
                                _logger.LogInformation(
                                    "ACME[{Dir}] challenge VALID for {Host}",
                                    Label,
                                    host);
                                break;
                            }

                            if (challenge.Status == ChallengeStatus.Invalid)
                            {
                                throw new InvalidOperationException(
                                    $"ACME authorization failed for {host}: " +
                                    $"{challenge.Error?.Type} {challenge.Error?.Detail}");
                            }

                            if (DateTimeOffset.UtcNow > challengeDeadline)
                            {
                                throw new TimeoutException(
                                    $"ACME challenge timed out for {host}");
                            }

                            await Task.Delay(1000, ct).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        _challengeStore.Remove(token);
                    }
                }

                var certificateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                var csrInfo = new CsrInfo { CommonName = host };

                var orderDeadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);
                var orderResource = await order.Resource().ConfigureAwait(false);

                while (orderResource.Status is OrderStatus.Pending or OrderStatus.Processing)
                {
                    ct.ThrowIfCancellationRequested();

                    if (DateTimeOffset.UtcNow > orderDeadline)
                    {
                        throw new TimeoutException(
                            $"ACME order not ready for {host} (status={orderResource.Status}).");
                    }

                    await Task.Delay(1000, ct).ConfigureAwait(false);
                    orderResource = await order.Resource().ConfigureAwait(false);
                }

                if (orderResource.Status != OrderStatus.Valid)
                    await order.Finalize(csrInfo, certificateKey).ConfigureAwait(false);

                orderDeadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);
                orderResource = await order.Resource().ConfigureAwait(false);

                while (orderResource.Status is
                       OrderStatus.Processing or
                       OrderStatus.Pending or
                       OrderStatus.Ready)
                {
                    ct.ThrowIfCancellationRequested();

                    if (DateTimeOffset.UtcNow > orderDeadline)
                    {
                        throw new TimeoutException(
                            $"ACME finalize timed out for {host} " +
                            $"(status={orderResource.Status}).");
                    }

                    await Task.Delay(1000, ct).ConfigureAwait(false);
                    orderResource = await order.Resource().ConfigureAwait(false);
                }

                if (orderResource.Status != OrderStatus.Valid)
                {
                    throw new InvalidOperationException(
                        $"ACME order did not become valid for {host} " +
                        $"(status={orderResource.Status}).");
                }

                var chain = await order.Download().ConfigureAwait(false);
                var (leafDer, issuersDer) = ExtractDerFromChain(chain);

                using var leafPublic = X509CertificateLoader.LoadCertificate(leafDer);
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportPkcs8PrivateKey(certificateKey.ToDer(), out _);

                using var leafWithKey = leafPublic.CopyWithPrivateKey(ecdsa);

                var pfxBuilder = new Pkcs12Builder();

                var leafBag = new Pkcs12SafeContents();
                leafBag.AddCertificate(leafWithKey);
                leafBag.AddShroudedKey(
                    ecdsa,
                    password: string.Empty,
                    new PbeParameters(
                        PbeEncryptionAlgorithm.Aes256Cbc,
                        HashAlgorithmName.SHA256,
                        100_000));

                pfxBuilder.AddSafeContentsUnencrypted(leafBag);

                if (issuersDer.Count > 0)
                {
                    var issuerBag = new Pkcs12SafeContents();
                    foreach (var der in issuersDer)
                    {
                        using var issuer = X509CertificateLoader.LoadCertificate(der);
                        issuerBag.AddCertificate(issuer);
                    }

                    pfxBuilder.AddSafeContentsUnencrypted(issuerBag);
                }

                pfxBuilder.SealWithMac(
                    string.Empty,
                    HashAlgorithmName.SHA256,
                    100_000);

                var pfxBytes = pfxBuilder.Encode();

                _logger.LogInformation(
                    "ACME[{Dir}] issued PFX for {Host} (len={Len})",
                    Label,
                    host,
                    pfxBytes.Length);

                return X509CertificateLoader.LoadPkcs12(
                    pfxBytes,
                    ReadOnlySpan<char>.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ACME[{Dir}] issuance FAILED for {Host}",
                    Label,
                    host);
                throw;
            }
        }

        private async Task RunPreflightAsync(
            string url,
            string keyAuthorization,
            CancellationToken ct)
        {
            try
            {
                using var client = new HttpClient(
                    new HttpClientHandler { AllowAutoRedirect = false })
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };

                using var response = await client.GetAsync(url, ct).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "Preflight GET {Url} -> {Status} len={Len}",
                    url,
                    (int)response.StatusCode,
                    body.Length);

                if (response.StatusCode == System.Net.HttpStatusCode.OK &&
                    !string.Equals(body, keyAuthorization, StringComparison.Ordinal))
                {
                    _logger.LogWarning(
                        "Preflight mismatch: body != keyAuthz (first 60) body='{Body}'",
                        body.Length > 60 ? body[..60] : body);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Preflight GET failed");
            }
        }

        private async Task<IKey> LoadOrCreateAccountKeyAsync(CancellationToken ct)
        {
            var directory = Path.GetDirectoryName(_accountPemPath)!;
            EnsureDirectory(directory);

            if (File.Exists(_accountPemPath))
            {
                HardenFile(_accountPemPath);
                _logger.LogInformation(
                    "ACME account key: using existing {Path}",
                    _accountPemPath);

                var pem = await File.ReadAllTextAsync(_accountPemPath, ct)
                    .ConfigureAwait(false);
                return KeyFactory.FromPem(pem);
            }

            _logger.LogInformation(
                "ACME account key: creating {Path}",
                _accountPemPath);

            var key = KeyFactory.NewKey(KeyAlgorithm.ES256);
            await WriteTextAtomicallyAsync(
                    _accountPemPath,
                    key.ToPem(),
                    ct)
                .ConfigureAwait(false);

            return key;
        }

        private static async Task WriteTextAtomicallyAsync(
            string path,
            string content,
            CancellationToken ct)
        {
            var directory = Path.GetDirectoryName(path)!;
            EnsureDirectory(directory);

            var tempPath = Path.Combine(
                directory,
                $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

            try
            {
                await using (var stream = new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await using var writer = new StreamWriter(
                        stream,
                        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                        bufferSize: 4096,
                        leaveOpen: true);

                    await writer.WriteAsync(content.AsMemory(), ct).ConfigureAwait(false);
                    await writer.FlushAsync(ct).ConfigureAwait(false);
                    stream.Flush(flushToDisk: true);
                }

                HardenFile(tempPath);

                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(
                            tempPath,
                            path,
                            destinationBackupFileName: null,
                            ignoreMetadataErrors: true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Move(tempPath, path, overwrite: true);
                    }
                    catch (IOException)
                    {
                        File.Move(tempPath, path, overwrite: true);
                    }
                }
                else
                {
                    File.Move(tempPath, path);
                }

                HardenFile(path);
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
                Directory.SetUnixFileMode(
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

        private static (byte[] LeafDer, List<byte[]> IssuersDer)
            ExtractDerFromChain(object certificateChain)
        {
            var type = certificateChain.GetType();

            var leafObject =
                type.GetProperty("Certificate")?.GetValue(certificateChain) ??
                type.GetProperty("Leaf")?.GetValue(certificateChain) ??
                throw new InvalidOperationException("CertificateChain leaf not found.");

            var toDer = leafObject.GetType().GetMethod("ToDer") ??
                        throw new InvalidOperationException("Leaf.ToDer() not found.");

            var leafDer = (byte[])toDer.Invoke(
                leafObject,
                Array.Empty<object>())!;

            var issuerPropertyNames = new[]
            {
                "Chain",
                "IssuerChain",
                "IssuerCertificates",
                "Certificates"
            };

            var issuersDer = new List<byte[]>();

            foreach (var propertyName in issuerPropertyNames)
            {
                var property = type.GetProperty(propertyName);
                if (property?.GetValue(certificateChain) is not
                    System.Collections.IEnumerable collection)
                {
                    continue;
                }

                foreach (var item in collection)
                {
                    if (item is null)
                        continue;

                    var method = item.GetType().GetMethod("ToDer");
                    if (method is null)
                        continue;

                    var der = (byte[])method.Invoke(
                        item,
                        Array.Empty<object>())!;

                    if (!der.AsSpan().SequenceEqual(leafDer))
                        issuersDer.Add(der);
                }

                break;
            }

            return (leafDer, issuersDer);
        }
    }
}
