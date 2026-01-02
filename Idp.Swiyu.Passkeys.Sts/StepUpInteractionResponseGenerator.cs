using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using System.Security.Claims;

namespace Idp.Swiyu.Passkeys.Sts;

public class StepUpInteractionResponseGenerator : AuthorizeInteractionResponseGenerator
{
    public StepUpInteractionResponseGenerator(
        IdentityServerOptions options,
        IClock clock,
        ILogger<AuthorizeInteractionResponseGenerator> logger,
        IConsentService consent,
        IProfileService profile) : base(options, clock, logger, consent, profile)
    {
    }

    protected override async Task<InteractionResponse> ProcessLoginAsync(ValidatedAuthorizeRequest request)
    {
        var result = await base.ProcessLoginAsync(request);

        if (!result.IsLogin && !result.IsError)
        {
            if (!AuthenticatedWithPasskeys(request.Subject!))
            {
                if (UserDeclinedMfa(request.Subject!))
                {
                    result.Error = OidcConstants.AuthorizeErrors.UnmetAuthenticationRequirements;
                }
                else
                {
                    result.RedirectUrl = "/Account/Login";
                }
            }
        }

        return result;
    }

    private bool PasskeysRequired(ValidatedAuthorizeRequest request) =>
       PasskeysRequestedByClient(request);

    private bool PasskeysRequestedByClient(ValidatedAuthorizeRequest request)
    {
        return request.AuthenticationContextReferenceClasses!.Contains("phr");
    }

    private bool AuthenticatedWithOpenIDVP(ClaimsPrincipal user) =>
        user.Claims.Any(c => c.Type == "amr" && c.Value ==  Amr.Mca);

    private bool AuthenticatedWithMfa(ClaimsPrincipal user) =>
        user.Claims.Any(c => c.Type == "amr" && (c.Value == Amr.Pop || c.Value ==  Amr.Mfa));

    private bool AuthenticatedWithPasskeys(ClaimsPrincipal user) =>
        user.Claims.Any(c => c.Type == "amr" && c.Value == Amr.Pop);

    private bool UserDeclinedMfa(ClaimsPrincipal user) =>
        user.Claims.Any(c => c.Type == "declined_mfa" && c.Value == "true");
}