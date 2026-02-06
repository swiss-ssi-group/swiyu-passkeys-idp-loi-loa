using Duende.IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Idp.Swiyu.Passkeys.Sts.SwiyuServices;

public class VerificationService
{
    private readonly ILogger<VerificationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string? _swiyuVerifierMgmtUrl;
    private readonly string? _issuerId;
    private readonly HttpClient _httpClient;

    public VerificationService(IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _swiyuVerifierMgmtUrl = "https://localhost:8084"; // configuration["SwiyuVerifierMgmtUrl"];
        _issuerId = configuration["ISSUER_ID"];
        _httpClient = httpClientFactory.CreateClient();
        _logger = loggerFactory.CreateLogger<VerificationService>();
        _configuration = configuration;
    }

    /// <summary>
    /// curl - X POST http://localhost:8082/management/api/verifications \
    ///       -H "accept: application/json" \
    ///       -H "Content-Type: application/json" \
    ///       -d '
    /// </summary>
    public async Task<string> CreateBetaIdVerificationPresentationAsync()
    {
        _logger.LogInformation("Creating verification presentation");

        // from "betaid-sdjwt"
        var acceptedIssuerDid = "did:tdw:QmPEZPhDFR4nEYSFK5bMnvECqdpf1tPTPJuWs9QrMjCumw:identifier-reg.trust-infra.swiyu-int.admin.ch:api:v1:did:9a5559f0-b81c-4368-a170-e7b4ae424527";

        var inputDescriptorsId = Guid.NewGuid().ToString();
        var presentationDefinitionId = "00000000-0000-0000-0000-000000000000"; // Guid.NewGuid().ToString();

        var json = GetBetaIdVerificationPresentationBody(inputDescriptorsId,
            presentationDefinitionId, acceptedIssuerDid, "betaid-sdjwt");

        // TODO sign the payload if JWT authentication is enabled on Swiyu  

        return await SendCreateVerificationPostRequest(json);
    }


    public async Task<VerificationManagementModel?> GetVerificationStatus(string verificationId)
    {
        var idEncoded = HttpUtility.UrlEncode(verificationId);
        using HttpResponseMessage response = await _httpClient.GetAsync(
            $"{_swiyuVerifierMgmtUrl}/management/api/verifications/{idEncoded}");

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (jsonResponse == null)
            {
                _logger.LogError("GetVerificationStatus no data returned from Swiyu");
                return null;
            }

            //  state: PENDING, SUCCESS, FAILED
            return JsonSerializer.Deserialize<VerificationManagementModel>(jsonResponse);
        }

        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError("Could not create verification presentation {vp}", error);

        throw new ArgumentException(error);
    }

    /// <summary>
    /// In a business app we can use the data from the verificationModel
    /// Verification data:
    /// Use: wallet_response/credential_subject_data
    ///
    /// birth_date, given_name, family_name, birth_place
    /// 
    /// </summary>
    /// <param name="verificationManagementModel"></param>
    /// <returns></returns>
    public VerificationClaims GetVerifiedClaims(VerificationManagementModel verificationManagementModel)
    {
        var json = verificationManagementModel.wallet_response!.credential_subject_data!.ToString();

        var jsonElement = JsonDocument.Parse(json!).RootElement;

        var claims = new VerificationClaims
        {
            BirthDate = jsonElement.GetProperty("birth_date").ToString(),
            BirthPlace = jsonElement.GetProperty("birth_place").ToString(),
            FamilyName = jsonElement.GetProperty("family_name").ToString(),
            GivenName = jsonElement.GetProperty("given_name").ToString()
        };

        return claims;
    }
    private async Task<string> SendCreateVerificationPostRequest(string json)
    {
        var accessToken = await VerificationServiceSecurityClient.RequestTokenAsync(_configuration);

        var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
        _httpClient.SetBearerToken(accessToken);
        var response = await _httpClient.PostAsync($"{_swiyuVerifierMgmtUrl}/management/api/verifications", jsonContent);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();

            return jsonResponse;
        }

        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError("Could not create verification presentation {vp}", error);

        throw new ArgumentException(error);
    }

    /// <summary>
    /// There will be private companies having a need to do identification routines (e.g. KYC or before issuing another credential), 
    /// asking for given_name, family_name, birth_date and birth_place.
    /// 
    /// { "path": [ "$.birth_date" ] },
    /// { "path": ["$.given_name"] },
    /// { "path": ["$.family_name"] },
    /// { "path": ["$.birth_place"] },
    /// </summary>
    private static string GetBetaIdVerificationPresentationBody(string inputDescriptorsId, string presentationDefinitionId, string acceptedIssuerDid, string vcType)
    {
        var json = $$"""
             {
                 "accepted_issuer_dids": [ "{{acceptedIssuerDid}}" ],
                 "response_mode": "direct_post",
                 "presentation_definition": {
                     "id": "{{presentationDefinitionId}}",
                     "input_descriptors": [
                         {
                             "id": "{{inputDescriptorsId}}",
                             "format": {
                                 "vc+sd-jwt": {
                                     "sd-jwt_alg_values": [
                                         "ES256"
                                     ],
                                     "kb-jwt_alg_values": [
                                         "ES256"
                                     ]
                                 }
                             },
                             "constraints": {
             	                "fields": [
             		                {
             			                "path": [
             				                "$.vct"
             			                ],
             			                "filter": {
             				                "type": "string",
             				                "const": "{{vcType}}"
             			                }
             		                },
             		                { "path": [ "$.birth_date" ] },
             		                { "path": [ "$.given_name" ] },
             		                { "path": [ "$.family_name" ] },
             		                { "path": [ "$.birth_place" ] }
             	                ]
                             }
                         }
                     ]
                 }
             }
             """;

        return json;
    }
}
