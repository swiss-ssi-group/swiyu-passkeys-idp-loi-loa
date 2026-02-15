using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace GenerateCertiticate;

class Program
{
    static CreateCertificates? _cc;
    static ImportExportCertificate? _iec;
    static void Main(string[] args)
    {
        var sp = new ServiceCollection()
           .AddCertificateManager()
           .BuildServiceProvider();

        _cc = sp.GetService<CreateCertificates>()!;
        _iec = sp.GetService<ImportExportCertificate>()!;

        var rsaCert = CreateRsaCertificate("localhost", 10);
        var ecdsaCert = CreateECDsaCertificate("localhost", 10);

        var pemPublicRsaKey = _iec.PemExportPublicKeyCertificate(rsaCert);
        File.WriteAllText("rsa256-public.pem", pemPublicRsaKey);

        using (RSA? rsa = rsaCert.GetRSAPrivateKey())
        {
            var pemPrivateRsaKey = rsa!.ExportRSAPrivateKeyPem();
            File.WriteAllText("rsa256-private.pem", pemPrivateRsaKey);
        }

        var pemPublicKey = _iec.PemExportPublicKeyCertificate(ecdsaCert);
        File.WriteAllText("ecdsa384-public.pem", pemPublicKey);

        using (ECDsa? ecdsa = ecdsaCert.GetECDsaPrivateKey())
        {
            var pemPrivateKey = ecdsa!.ExportECPrivateKeyPem();
            File.WriteAllText("ecdsa384-private.pem", pemPrivateKey);
        }

        var publicEcJwk = ExportEcJwk(ecdsaCert, "ec-swiyu-signing", true);
        File.WriteAllText("ecdsa384-public.jwk", publicEcJwk);
        Console.WriteLine(publicEcJwk);

        Console.WriteLine("created, keys are in the bin folder");
    }

    public static X509Certificate2 CreateRsaCertificate(string dnsName, int validityPeriodInYears)
    {
        var basicConstraints = new BasicConstraints
        {
            CertificateAuthority = false,
            HasPathLengthConstraint = false,
            PathLengthConstraint = 0,
            Critical = false
        };

        var subjectAlternativeName = new SubjectAlternativeName
        {
            DnsName = new List<string>
            {
                dnsName,
            }
        };

        var x509KeyUsageFlags = X509KeyUsageFlags.DigitalSignature;

        // only if certification authentication is used
        var enhancedKeyUsages = new OidCollection
        {
            OidLookup.ClientAuthentication,
            OidLookup.ServerAuthentication 
            // OidLookup.CodeSigning,
            // OidLookup.SecureEmail,
            // OidLookup.TimeStamping  
        };

        var certificate = _cc!.NewRsaSelfSignedCertificate(
            new DistinguishedName { CommonName = dnsName },
            basicConstraints,
            new ValidityPeriod
            {
                ValidFrom = DateTimeOffset.UtcNow,
                ValidTo = DateTimeOffset.UtcNow.AddYears(validityPeriodInYears)
            },
            subjectAlternativeName,
            enhancedKeyUsages,
            x509KeyUsageFlags,
            new RsaConfiguration
            {
                KeySize = 2048,
                HashAlgorithmName = HashAlgorithmName.SHA256
            });

        return certificate;
    }

    public static X509Certificate2 CreateECDsaCertificate(string dnsName, int validityPeriodInYears)
    {
        var basicConstraints = new BasicConstraints
        {
            CertificateAuthority = false,
            HasPathLengthConstraint = false,
            PathLengthConstraint = 0,
            Critical = false
        };

        var subjectAlternativeName = new SubjectAlternativeName
        {
            DnsName = new List<string>
            {
                dnsName,
            }
        };

        var x509KeyUsageFlags = X509KeyUsageFlags.DigitalSignature;

        // only if certification authentication is used
        var enhancedKeyUsages = new OidCollection {
            OidLookup.ClientAuthentication,
            OidLookup.ServerAuthentication 
            // OidLookup.CodeSigning,
            // OidLookup.SecureEmail,
            // OidLookup.TimeStamping 
        };

        var certificate = _cc!.NewECDsaSelfSignedCertificate(
            new DistinguishedName { CommonName = dnsName },
            basicConstraints,
            new ValidityPeriod
            {
                ValidFrom = DateTimeOffset.UtcNow,
                ValidTo = DateTimeOffset.UtcNow.AddYears(validityPeriodInYears)
            },
            subjectAlternativeName,
            enhancedKeyUsages,
            x509KeyUsageFlags,
            new ECDsaConfiguration
            {
                KeySize = 384,
                HashAlgorithmName = HashAlgorithmName.SHA384
            });

        return certificate;
    }

    public string ExportRsaJwk(X509Certificate2 cert, string? keyId = null)
    {
        using var rsa = cert.GetRSAPublicKey() ?? throw new InvalidOperationException("No RSA public key.");
        var p = rsa.ExportParameters(false);
        string n = Base64UrlEncode(p.Modulus!);
        string e = Base64UrlEncode(p.Exponent!);

        var jwk = new
        {
            kty = "RSA",
            n,
            e,
            use = "sig",
            alg = "RS256", // adjust to your signing algorithm
            kid = keyId // optional
        };

        return JsonSerializer.Serialize(jwk, new JsonSerializerOptions { WriteIndented = true });

        static string Base64UrlEncode(byte[] input)
            => Convert.ToBase64String(input)
                       .TrimEnd('=')
                       .Replace('+', '-')
                       .Replace('/', '_');
    }
    
    public static string ExportEcJwk(X509Certificate2 cert, string? kid = null, bool pretty = true)
    {
        using var ecdsa = cert.GetECDsaPublicKey()
            ?? throw new InvalidOperationException("Certificate does not contain an ECDSA public key.");

        var p = ecdsa.ExportParameters(false);

        string crv = p.Curve.Oid.FriendlyName switch
        {
            "nistP256" => "P-256",
            "nistP384" => "P-384",
            "nistP521" => "P-521",
            _ => throw new NotSupportedException($"Unsupported EC curve: {p.Curve.Oid.FriendlyName} (OID: {p.Curve.Oid.Value})")
        };

        string x = Base64UrlEncode(p.Q.X!);
        string y = Base64UrlEncode(p.Q.Y!);

        // Choose a default alg consistent with curve; adjust if you sign with a different alg.
        string alg = crv switch
        {
            "P-256" => "ES256",
            "P-384" => "ES384",
            "P-521" => "ES512",
            _ => "ES256"
        };

        var jwk = new
        {
            kty = "EC",
            crv,
            x,
            y,
            use = "sig",
            alg,
            kid // optional but recommended
        };

        return JsonSerializer.Serialize(jwk, new JsonSerializerOptions { WriteIndented = pretty });
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input)
               .TrimEnd('=')
               .Replace('+', '-')
               .Replace('/', '_');
}

