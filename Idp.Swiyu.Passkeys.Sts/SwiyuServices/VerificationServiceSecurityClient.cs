using Duende.IdentityModel.Client;

namespace Idp.Swiyu.Passkeys.Sts.SwiyuServices;
public class VerificationServiceSecurityClient
{
    public static async Task<TokenResponse> RequestTokenAsync()
    {
        var client = new HttpClient();

        // TODO use address
        var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5001");
        
        if (disco.IsError) throw new Exception(disco.Error);

        var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = "swiyu-client",
            ClientSecret = "SLlwqdedF4f289k$3eDa23ed0iTk4RaDtttk23d08nhzd",
            Scope = "swiyu", 
        });

        if (response.IsError) throw new Exception(response.Error);

        return response;
    }
}
