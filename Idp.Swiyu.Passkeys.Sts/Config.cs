// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;

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
        ];

    public static IEnumerable<ApiResource> GetApiResources()
    {
        return new List<ApiResource>
    {
        new ApiResource("dpop-api", "DPoP API")
        {
            Scopes = { "scope2" }
        }
    };
    }
    public static IEnumerable<Client> Clients =>
        [
            // interactive client using code flow + pkce + par + DPoP
            new Client
            {
                ClientId = "web-client",
                ClientSecrets = { new Secret("super-secret-$123".Sha256()) },

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
        ];
}
