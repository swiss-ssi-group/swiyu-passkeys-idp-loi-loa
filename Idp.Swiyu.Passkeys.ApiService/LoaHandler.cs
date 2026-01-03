using Idp.Swiyu.Passkeys.ApiService;
using Microsoft.AspNetCore.Authorization;

public class LoaHandler : AuthorizationHandler<LoaRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LoaRequirement requirement)
    {
        // DPoP is required to use the API
        var loa = context.User.FindFirst(c => c.Type == Consts.LOA);

        if (loa is null)
        {
            return Task.CompletedTask;
        }

        // Lets require passkeys to use this API
        if (loa.Value != Consts.LOA_400)
        {
            return Task.CompletedTask;
        }

        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}