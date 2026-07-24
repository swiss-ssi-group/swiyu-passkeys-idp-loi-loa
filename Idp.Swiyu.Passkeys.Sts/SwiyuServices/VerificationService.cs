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
        _swiyuVerifierMgmtUrl = configuration["SwiyuVerifierMgmtUrl"];
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

        var json = GetBetaIdVerificationPresentationBodyV4(inputDescriptorsId,
            presentationDefinitionId, acceptedIssuerDid, "betaid-sdjwt");

        // TODO sign the payload if JWT authentication is enabled on Swiyu  

        return await SendCreateVerificationPostRequest(json);
    }

    public async Task<VerificationManagementModel?> GetVerificationStatus(string verificationId)
    {
        var accessToken = await VerificationServiceSecurityClient.RequestTokenAsync(_configuration);
        _httpClient.SetBearerToken(accessToken);

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
            else if(jsonResponse.Contains("FAILED"))
            {
                _logger.LogInformation("GetVerificationStatus verificationId FAILED: {jsonResponse}", jsonResponse);
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
    /// > **Note:** The verifier accepts both `dc+sd-jwt` (current spec, SD-JWT VC Draft 06+, per [draft-ietf-oauth-sd-jwt-vc-09 §A.2.1](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-sd-jwt-vc-09#name-application-dcsd-jwt)) 
    /// and `vc+sd-jwt` (legacy SD-JWT VC drafts ≤ 05) on the credential's `typ` header.
    /// There will be private companies having a need to do identification routines (e.g. KYC or before issuing another credential), 
    /// asking for given_name, family_name, birth_date and birth_place.
    /// 
    /// { "path": [ "$.birth_date" ] },
    /// { "path": ["$.given_name"] },
    /// { "path": ["$.family_name"] },
    /// { "path": ["$.birth_place"] },
    /// </summary>
    private static string GetBetaIdVerificationPresentationBodyV4(string inputDescriptorsId, string presentationDefinitionId, string acceptedIssuerDid, string vcType)
    {
        var json = $$"""
             {
                 "accepted_issuer_dids": [ "{{acceptedIssuerDid}}", "did:webvh:QmQNMXCBYHLsH5zJeE1hC6tn7GpQFfvqJaWPqwpn7pafcy:identifier-reg-a.trust-infra.swiyu-int.admin.ch:api:v1:did:3d20b010-8d39-4cdd-b5cd-a6356b4e1218" ],
                 "jwt_secured_authorization_request": true,
                 "response_mode": "direct_post",
                 "verification_purpose": {
                   "scope": "ch.identity",
                   "purpose_name": {
                     "default": "Identity verification"
                   },
                   "purpose_description": {
                     "default": "Used to verify the identity of an individual"
                   }
                 },
                 "dcql_query": {
                   "credentials": [
                     {
                       "id": "{{presentationDefinitionId}}",
                       "format": "dc+sd-jwt",
                       "meta": {
                         "vct_values": ["betaid-sdjwt", "urn:vct:ch.admin.bcs.betaid"]
                       },
                       "claims": [
                         { "path": [ "$.birth_date" ] },
             		     { "path": [ "$.given_name" ] },
             		     { "path": [ "$.family_name" ] },
             		     { "path": [ "$.birth_place" ] }
                       ],
                       "require_cryptographic_holder_binding": true
                     }
                   ]
                 }
             }
             """;

        return json;
    }
}
