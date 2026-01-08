using Duende.AccessTokenManagement.DPoP;
using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.IdentityModel;
using Idp.Swiyu.Passkeys.Web;
using Idp.Swiyu.Passkeys.Web.Components;
using Idp.Swiyu.Passkeys.Web.WeatherServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

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

    options.Events = OidcEventHandlers.OidcEvents(builder.Configuration);

    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.ResponseType = OpenIdConnectResponseType.Code;

    //options.ClientSecret = "test";

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
});

// add service to create assertions for token management
//builder.Services.AddTransient<IClientAssertionService, ClientAssertionService>();

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