using Duende.AccessTokenManagement.DPoP;
using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.IdentityModel;
using Idp.Swiyu.Passkeys.Web;
using Idp.Swiyu.Passkeys.Web.Components;
using Idp.Swiyu.Passkeys.Web.WeatherServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Logging.AddConsole();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddOutputCache();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<WeatherApiClient>();

//var privatePem = File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "ecdsa384-private.pem"));
//var publicPem = File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "ecdsa384-public.pem"));

var webDpopClientPrivatePem = builder.Configuration.GetValue<string>("WebDpopClientPrivatePem");
var webDpopClientPublicPem = builder.Configuration.GetValue<string>("WebDpopClientPublicPem");

var ecdsaCertificate = X509Certificate2.CreateFromPem(webDpopClientPublicPem, webDpopClientPrivatePem);
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
    options.Events = OidcEventHandlers.OidcEvents(builder.Configuration);

    options.ClientId = builder.Configuration["WebOidcClientId"];
    options.Authority = builder.Configuration["WebOidcAuthority"];
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.ResponseType = OpenIdConnectResponseType.Code;

    // client_assertion used, set in oidc events
    //options.ClientSecret = "test";

    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.MapInboundClaims = false;

    options.ClaimActions.MapUniqueJsonKey("loa", "loa");
    options.ClaimActions.MapUniqueJsonKey("loi", "loi");
    options.ClaimActions.MapUniqueJsonKey(JwtClaimTypes.Email, JwtClaimTypes.Email);

    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Require;

    options.Scope.Add("scope2");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "name"
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
    client.BaseAddress = new("https+http://api-service");
});

builder.Services.AddSecurityHeaderPolicies()
    .SetDefaultPolicy(SecurityHeadersDefinitions
    .GetHeaderPolicyCollection(builder.Configuration["WebOidcAuthority"],
        builder.Environment.IsDevelopment()));

builder.Services.AddAuthenticationCore();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

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

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.MapLoginLogoutEndpoints();

app.Run();