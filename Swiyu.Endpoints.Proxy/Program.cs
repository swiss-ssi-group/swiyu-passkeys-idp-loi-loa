using Swiyu.Endpoints.Proxy;
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromMemory(YarpConfigurations.GetVerifierRoutes(),
        YarpConfigurations.GetVerifierClusters(
            builder.Configuration["SwiyuVerifierMgmtUrl"]!));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapReverseProxy();

app.UseStaticFiles();

app.Run();

// Proxy Endpoints:
// https://localhost:7009/.well-known/openid-configuration
// https://swiyu-endpoints-proxy.livelysand-4f5c661d.switzerlandnorth.azurecontainerapps.io/.well-known/openid-configuration

// https://swiyu-endpoints-proxy.livelysand-4f5c661d.switzerlandnorth.azurecontainerapps.io/issuer_openidconfigfile.json