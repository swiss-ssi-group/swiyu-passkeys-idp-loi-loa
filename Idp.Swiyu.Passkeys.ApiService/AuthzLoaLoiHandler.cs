using Microsoft.AspNetCore.Authorization;

public class AuthzLoaLoiHandler : AuthorizationHandler<AuthzLoaLoiRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthzLoaLoiRequirement requirement)
    {
        var loa = context.User.FindFirst(c => c.Type == "loa");
        var loi = context.User.FindFirst(c => c.Type == "loi");

        if (loa is null || loi is null)
        {
            return Task.CompletedTask;
        }

        // Lets require passkeys to use this API
        // DPoP is required to use the API
        if (loa.Value != "loa.400")
        {
            return Task.CompletedTask;
        }

        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}