using Duende.IdentityModel.Client;
using Microsoft.Identity.Client;

namespace Idp.Swiyu.Passkeys.Sts.SwiyuServices;
public class VerificationServiceSecurityClient
{
    /// <summary>
    /// MSAL
    /// </summary>
    public static async Task<string> RequestTokenAsync(IConfiguration configuration)
    {
        // 1. Client client credentials client
        var app = ConfidentialClientApplicationBuilder
            .Create(configuration["SwiyuManagementClientId"])
            .WithClientSecret(configuration["SwiyuManagementClientSecret"])
            .WithAuthority(configuration["SwiyuManagementAuthority"])
            .Build();

        var scopes = new[] { configuration["SwiyuManagementScope"] };

        // 2. Get access token
        var authResult = await app.AcquireTokenForClient(scopes)
            .ExecuteAsync();

        return authResult.AccessToken;

    }

    // OAuth
    //public static async Task<TokenResponse> RequestTokenAsync()
    //{
    //    var client = new HttpClient();

    //    // TODO use address
    //    var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5001");

    //    if (disco.IsError) throw new Exception(disco.Error);

    //    var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
    //    {
    //        Address = disco.TokenEndpoint,
    //        ClientId = "swiyu-client",
    //        ClientSecret = "SLlwqdedF4f289k$3eDa23ed0iTk4RaDtttk23d08nhzd",
    //        Scope = "swiyu", 
    //    });

    //    if (response.IsError) throw new Exception(response.Error);

    //    return response;
    //}
}
