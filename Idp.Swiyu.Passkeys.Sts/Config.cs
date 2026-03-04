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
            new ApiScope("scope2")
        ];

    public static IEnumerable<ApiResource> GetApiResources()
    {
        return
        [
            new ApiResource("dpop-api", "DPoP API")
            {
                Scopes = { "scope2" }
            },
        ];
    }

    public static IEnumerable<Client> Clients(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var webClientUrl = configuration.GetValue<string>("WebClientUrl");
        var stsOidcWebClientPublicPem = configuration.GetValue<string>("StsOidcWebClientPublicPem");
        var rsaCertificate = X509Certificate2.CreateFromPem(stsOidcWebClientPublicPem);

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

                RedirectUris = { $"{webClientUrl}/signin-oidc" },
                FrontChannelLogoutUri = $"{webClientUrl}/signout-oidc",
                PostLogoutRedirectUris = { $"{webClientUrl}/signout-callback-oidc" },

                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "scope2" }
            }
        ];
    }
}
