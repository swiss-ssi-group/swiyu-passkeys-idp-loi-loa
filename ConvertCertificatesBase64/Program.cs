using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace ConvertCertiticatesBase64;

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

        var assertKeyPrivatePem = File.ReadAllText(Path.Combine("../../../../SwiyuTools/SwiyuDidToolbox/.didtoolbox", "assert-key-01"));
        File.WriteAllText("assert-key-01-issuer.pem.base64", ConfigConverter.CreateBase64FromPem(assertKeyPrivatePem));

        var authKeyPrivatePem = File.ReadAllText(Path.Combine("../../../../SwiyuTools/SwiyuDidToolbox/.didtoolbox", "auth-key-01"));
        File.WriteAllText("auth-key-01-verifier.pem.base64", ConfigConverter.CreateBase64FromPem(authKeyPrivatePem));

        Console.WriteLine("Converted, keys are in the bin folder");
    }
}

