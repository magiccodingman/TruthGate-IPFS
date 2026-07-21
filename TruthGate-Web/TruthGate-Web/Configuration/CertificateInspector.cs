using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace TruthGate_Web.Configuration
{
    public enum CertificateState
    {
        Fresh,
        RenewalDue,
        Expired,
        Missing,
        Corrupt,
        WrongHostname,
        MissingPrivateKey,
        NotYetValid
    }

    public readonly record struct CertificateInspection(
        CertificateState State,
        DateTimeOffset? NotBefore,
        DateTimeOffset? NotAfter,
        string? Detail = null)
    {
        public bool CanServe => State is CertificateState.Fresh or CertificateState.RenewalDue;
        public bool NeedsIssuance => State != CertificateState.Fresh;
    }

    public static class CertificateInspector
    {
        public static CertificateInspection Inspect(
            X509Certificate2? certificate,
            string host,
            DateTimeOffset now,
            TimeSpan renewalWindow)
        {
            if (certificate is null)
                return new CertificateInspection(CertificateState.Missing, null, null, "No certificate is stored.");

            var notBefore = new DateTimeOffset(certificate.NotBefore.ToUniversalTime());
            var notAfter = new DateTimeOffset(certificate.NotAfter.ToUniversalTime());

            if (!certificate.HasPrivateKey)
                return new CertificateInspection(
                    CertificateState.MissingPrivateKey,
                    notBefore,
                    notAfter,
                    "The certificate does not contain a private key.");

            var normalizedHost = NormalizeHost(host);
            bool matches;
            try
            {
                matches = certificate.MatchesHostname(
                    normalizedHost,
                    allowWildcards: true,
                    allowCommonName: false);
            }
            catch (Exception ex)
            {
                return new CertificateInspection(
                    CertificateState.Corrupt,
                    notBefore,
                    notAfter,
                    $"Hostname validation failed: {ex.Message}");
            }

            if (!matches)
                return new CertificateInspection(
                    CertificateState.WrongHostname,
                    notBefore,
                    notAfter,
                    $"The certificate SAN does not match '{normalizedHost}'.");

            if (now < notBefore)
                return new CertificateInspection(
                    CertificateState.NotYetValid,
                    notBefore,
                    notAfter,
                    "The certificate is not valid yet.");

            if (now >= notAfter)
                return new CertificateInspection(
                    CertificateState.Expired,
                    notBefore,
                    notAfter,
                    "The certificate has expired.");

            if (notAfter - now <= renewalWindow)
                return new CertificateInspection(
                    CertificateState.RenewalDue,
                    notBefore,
                    notAfter,
                    "The certificate is valid and should be renewed in the background.");

            return new CertificateInspection(CertificateState.Fresh, notBefore, notAfter);
        }

        public static string NormalizeHost(string host)
        {
            var normalized = (host ?? string.Empty).Trim().TrimEnd('.').ToLowerInvariant();
            if (normalized.Length == 0)
                return normalized;

            try
            {
                return new IdnMapping().GetAscii(normalized);
            }
            catch
            {
                return normalized;
            }
        }
    }
}
