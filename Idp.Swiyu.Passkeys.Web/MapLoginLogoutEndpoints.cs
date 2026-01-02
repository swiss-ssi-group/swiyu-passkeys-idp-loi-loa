using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Primitives;

namespace Idp.Swiyu.Passkeys.Web;

public static class LoginLogoutEndpoints
{
    public static WebApplication MapLoginLogoutEndpoints(this WebApplication app)
    {
        app.MapGet("/login", async context =>
        {
            var returnUrl = context.Request.Query["returnUrl"];

            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = returnUrl == StringValues.Empty ? "/" : returnUrl.ToString()
            });
        }).AllowAnonymous();

        app.MapGet("/stepuploa", async context =>
        {
            var returnUrl = context.Request.Query["returnUrl"];
            var loa = context.Request.Query["loa"];

            if (string.IsNullOrEmpty(loa) && loa == "loi.400")
            {
                await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
                {
                    RedirectUri = returnUrl == StringValues.Empty ? "/" : returnUrl.ToString(),
                    Items = { ["arc_values"] = "phr" }
                });
            }
            else
            {
                await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
                {
                    RedirectUri = returnUrl == StringValues.Empty ? "/" : returnUrl.ToString(),
                    Items = { ["arc_values"] = "mfa" }
                });
            }
 
        }).AllowAnonymous();

        app.MapPost("/logout", async context =>
        {
            if (context.User.Identity?.IsAuthenticated ?? false)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            }
            else
            {
                context.Response.Redirect("/");
            }
        });

        return app;
    }

}
