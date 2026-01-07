using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.DPoP;
using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Idp.Swiyu.Passkeys.Web;
using Idp.Swiyu.Passkeys.Web.Components;
using Idp.Swiyu.Passkeys.Web.WeatherServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using static Duende.AccessTokenManagement.AccessTokenRequestHandler;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<WeatherApiClient>();

var oidcConfig = builder.Configuration.GetSection("OpenIDConnectSettings");

var privatePem = File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "ecdsa384-private.pem"));
var publicPem = File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "ecdsa384-public.pem"));
var ecdsaCertificate = X509Certificate2.CreateFromPem(publicPem, privatePem);
var ecdsaCertificateKey = new ECDsaSecurityKey(ecdsaCertificate.GetECDsaPrivateKey());

var clientAssertionJwt = CreateClientToken("web-client", "https://localhost:5001");

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "__Host-idp-swiyu-passkeys-web";
    options.Cookie.SameSite = SameSiteMode.Lax;
    // can be strict if same-site
    //options.Cookie.SameSite = SameSiteMode.Strict;
})
.AddOpenIdConnect(options =>
{
    builder.Configuration.GetSection("OpenIDConnectSettings").Bind(options);

    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.ResponseType = OpenIdConnectResponseType.Code;

    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.MapInboundClaims = false;

    options.ClaimActions.MapUniqueJsonKey("loa", "loa");
    options.ClaimActions.MapUniqueJsonKey("loi", "loi");
    options.ClaimActions.MapUniqueJsonKey(JwtClaimTypes.Email, JwtClaimTypes.Email);

    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;

    options.Scope.Add("scope2");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "name"
    };

    options.Events = new OpenIdConnectEvents
    {
        // Add client_assertion            
        OnAuthorizationCodeReceived = context =>
        {
            // https://openid.net/specs/openid-connect-eap-acr-values-1_0-final.html
            if (context.Properties.Items.ContainsKey("acr_values"))
            {
                context.ProtocolMessage.AcrValues = context.Properties.Items["acr_values"];
            }

            context.TokenEndpointRequest!.ClientAssertion = clientAssertionJwt;
            context.TokenEndpointRequest.ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

            return Task.CompletedTask;
        },
        //OnRedirectToIdentityProvider = context =>
        //{
        //    // https://openid.net/specs/openid-connect-eap-acr-values-1_0-final.html
        //    context.ProtocolMessage.AcrValues = "phr";
        //    // context.ProtocolMessage.AcrValues = "http://schemas.openid.net/pape/policies/2007/06/multi-factor";
        //    if (context.Properties.Items.ContainsKey("acr_values"))
        //    {
        //        context.ProtocolMessage.AcrValues = context.Properties.Items["acr_values"];
        //    }
        //    return Task.CompletedTask;
        //},
        //OnPushAuthorization = context =>
        //{
        //    context.ProtocolMessage.Parameters.Add("client_assertion", clientAssertion);
        //    context.ProtocolMessage.Parameters.Add("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
        //    // https://openid.net/specs/openid-connect-eap-acr-values-1_0-final.html
        //    if (context.Properties.Items.ContainsKey("acr_values"))
        //    {
        //        context.ProtocolMessage.AcrValues = context.Properties.Items["acr_values"];
        //    }
        //    return Task.CompletedTask;
        //},
        OnTokenResponseReceived = context =>
        {
            var idToken = context.TokenEndpointResponse.IdToken;
            var accessToken = context.TokenEndpointResponse.AccessToken;
            return Task.CompletedTask;
        },
        OnUserInformationReceived = context =>
        {
            return Task.CompletedTask;
        }
    };
});

// add automatic token management
builder.Services.AddOpenIdConnectAccessTokenManagement(options =>
{
    // create and configure a DPoP JWK
    //var rsaKey = new RsaSecurityKey(RSA.Create(2048));
    //var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(rsaKey);
    //jwk.Alg = "PS256";
    //options.DPoPJsonWebKey = JsonSerializer.Serialize(jwk);

    //var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(rsaCertificateKey);
    //jwk.Alg = "PS256";
    //options.DPoPJsonWebKey = JsonSerializer.Serialize(jwk);

    var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(ecdsaCertificateKey);
    jwk.Alg = "ES384";
    options.DPoPJsonWebKey = DPoPProofKey.ParseOrDefault(JsonSerializer.Serialize(jwk));
});

builder.Services.AddUserAccessTokenHttpClient("dpop-api-client", configureClient: client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

builder.Services.AddSecurityHeaderPolicies()
    .SetDefaultPolicy(SecurityHeadersDefinitions
    .GetHeaderPolicyCollection(oidcConfig["Authority"],
        builder.Environment.IsDevelopment()));

builder.Services.AddAuthenticationCore();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddHealthChecks();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    IdentityModelEventSource.ShowPII = true;
    IdentityModelEventSource.LogCompleteSecurityArtifact = true;
}

app.UseSecurityHeaders();

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.MapLoginLogoutEndpoints();

app.MapHealthChecks("/health");

app.Run();

string CreateClientToken(string clientId, string audience)
{
    var now = DateTime.UtcNow;
    // client assertion
    var privatePem = File.ReadAllText(Path.Combine("", "rsa256-private.pem"));
    var publicPem = File.ReadAllText(Path.Combine("", "rsa256-public.pem"));
    var rsaCertificate = X509Certificate2.CreateFromPem(publicPem, privatePem);
    var rsaCertificateKey = new RsaSecurityKey(rsaCertificate.GetRSAPrivateKey());
    var signingCredentials = new SigningCredentials(new X509SecurityKey(rsaCertificate), "RS256");

    var token = new JwtSecurityToken(
        clientId,
        audience,
        new List<Claim>()
        {
                new Claim(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                new Claim(JwtClaimTypes.Subject, clientId),
                new Claim(JwtClaimTypes.IssuedAt, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        },
        now,
        now.AddMinutes(1),
        signingCredentials
    );

    token.Header[JwtClaimTypes.TokenType] = "client-authentication+jwt";

    var tokenHandler = new JwtSecurityTokenHandler();
    tokenHandler.OutboundClaimTypeMap.Clear();

    return tokenHandler.WriteToken(token);
}