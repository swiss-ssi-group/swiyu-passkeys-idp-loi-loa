using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using System.Text;

namespace Idp.Swiyu.Passkeys.ApiService;

/// <summary>
/// https://datatracker.ietf.org/doc/rfc9470/ 
/// implementation for step-up authorization requirements
/// </summary>
public class ForbiddenAuthorizationMiddleware : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authResult)
    {
        // If the authorization was forbidden due to a step-up requirement, set
        // the status code and WWW-Authenticate header to indicate that step-up
        // is required
        if (authResult.Forbidden)
        {
            var loaFailed = authResult.AuthorizationFailure!.FailedRequirements
                .OfType<LoaRequirement>().FirstOrDefault();
            var loiFailed = authResult.AuthorizationFailure!.FailedRequirements
                .OfType<LoiRequirement>().FirstOrDefault();

            if (loaFailed != null || loiFailed != null)
            {
                var errorMessage = new CreateErrorMessage();
                if (loaFailed != null)
                {
                    errorMessage.Loa = Consts.LOA_400;
                }
                if (loiFailed != null)
                {
                    errorMessage.Loi = Consts.LOI_400;
                }

                context.Response.Headers.WWWAuthenticate = errorMessage.GetErrorMessage();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                return;
            }
        }

        // Fall back to the default implementation.
        await defaultHandler.HandleAsync(next, context, policy, authResult);
    }
}

public class CreateErrorMessage
{
    private readonly string Error = "insufficient_user_authentication";
    private string ErrorDescription
    {
        get
        {
            var errorDescription = new StringBuilder();

            if (Loi != null && Loa != null)
            {
                errorDescription.Append("insufficient level of identification and authentication");
            }

            if (Loi != null && Loa == null)
            {
                errorDescription.Append("insufficient level of identification");
            }

            if (Loa != null && Loi == null)
            {
                errorDescription.Append("insufficient level of authentication");
            }


            return errorDescription.ToString();
        }
    }

    public string? Loi { get; set; }
    public string? Loa { get; set; }

    public string GetErrorMessage()
    {
        var props = new StringBuilder();
        props.Append($"Bearer error=\"{Error}\",");
        props.Append($"error_description=\"{ErrorDescription}\", ");

        if (Loi != null && Loa != null)
        {
            props.Append($"{Consts.LOI}=\"{Loi}\", ");
            props.Append($"{Consts.LOA}=\"{Loa}\"");
        }

        if (Loi != null && Loa == null)
        {
            props.Append($"{Consts.LOI}=\"{Loi}\"");
        }

        if (Loa != null && Loi == null)
        {
            props.Append($"{Consts.LOA}=\"{Loa}\"");
        }

        return props.ToString();
    }
}
