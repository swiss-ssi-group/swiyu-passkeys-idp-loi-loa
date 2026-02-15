// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using System.Security.Cryptography.X509Certificates;

namespace Idp.Swiyu.Passkeys.Sts;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        [
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        ];

    public static IEnumerable<ApiScope> ApiScopes =>
        [
            new ApiScope("scope2"),
            new ApiScope("swiyu"),
        ];

    public static IEnumerable<ApiResource> GetApiResources()
    {
        return
        [
            new ApiResource("dpop-api", "DPoP API")
            {
                Scopes = { "scope2" }
            },
            new ApiResource("verifier-api", "Swiyu Verifier")
            {
                Scopes = { "swiyu" }
            }
        ];
    }

    public static IEnumerable<Client> Clients(IWebHostEnvironment environment)
    {
        var publicPem = File.ReadAllText(Path.Combine(environment.ContentRootPath, "rsa256-public.pem"));
        var rsaCertificate = X509Certificate2.CreateFromPem(publicPem);

        // interactive client using code flow + pkce + par + DPoP
        return [
            new Client
            {
                ClientId = "webclient",
                ClientSecrets =
                {
                        //new Secret("test".Sha256()),
                        new Secret
                        {
                            // X509 cert base64-encoded
                            Type = IdentityServerConstants.SecretTypes.X509CertificateBase64,
                            Value = Convert.ToBase64String(rsaCertificate.GetRawCertData())
                        }
                },
                RequireDPoP = true,
                RequirePushedAuthorization = true,

                AllowedGrantTypes = GrantTypes.Code,
                AlwaysIncludeUserClaimsInIdToken = true,

                RedirectUris = { "https://localhost:7019/signin-oidc" },
                FrontChannelLogoutUri = "https://localhost:7019/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:7019/signout-callback-oidc" },

                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "scope2" }
            },
            new Client
            {
                ClientId = "swiyu-client",
                ClientName = "swiyu client", 
       
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("SLlwqdedF4f289k$3eDa23ed0iTk4RaDtttk23d08nhzd".Sha256()) },

                AllowedScopes = { "swiyu" }
            },
        ];
    }
}
