using Idp.Swiyu.Passkeys.ApiService;
using Microsoft.AspNetCore.Authorization;

public class LoiHandler : AuthorizationHandler<LoiRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LoiRequirement requirement)
    {
        // DPoP is required to use the API
        var loi = context.User.FindFirst(c => c.Type == Consts.LOI);

        if (loi is null)
        {
            return Task.CompletedTask;
        }

        // Lets require swiyu identified to use this API
        if (loi.Value != Consts.LOI_400)
        {
            return Task.CompletedTask;
        }

        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}