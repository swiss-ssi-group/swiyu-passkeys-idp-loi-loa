using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

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
                var header = new CreateWWWAuthenticateHeader();
                if (loaFailed != null)
                {
                    header.AcrValues = "pop";
                    header.Loa = "loa.400";
                }
                if (loiFailed != null)
                {
                    header.Loi = "loi.400";
                }

                context.Response.Headers.WWWAuthenticate = header.ToString();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        // Fall back to the default implementation.
        await defaultHandler.HandleAsync(next, context, policy, authResult);
    }
}

public class CreateWWWAuthenticateHeader
{
    private readonly string Error = "insufficient_user_authentication";
    private string ErrorDescription
    {
        get
        {
            var ret = string.Empty;
            if (Loi != null)
            {
                ret += "insufficient level of identification. ";
            }
            if (Loa != null)
            {
                ret += "insufficient level of authentication. ";
            }
            if (AcrValues != null)
            {
                ret += "Passkeys authentication is required.";
            }
            return ret;
        }
    }

    public string? Loi { get; set; }
    public string? Loa { get; set; }
    public string? AcrValues { get; set; }

    public override string ToString()
    {
        var props = new List<string> {
            $"Bearer error=\"{Error}\"",
            $"error_description=\"{ErrorDescription}\""
        };
        if (Loi != null)
        {
            props.Add($"loi={Loi}");
        }
        if (Loa != null)
        {
            props.Add($"loa={Loa}");
        }
        if (AcrValues != null)
        {
            props.Add($"acr_values={AcrValues}");
        }
        return string.Join(',', props);
    }
}
