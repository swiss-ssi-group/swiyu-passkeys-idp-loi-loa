using Swiyu.Endpoints.Proxy;
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register logging transform provider
builder.Services.AddSingleton<SwiyuLoggingTransformProvider>();

builder.Services.AddReverseProxy()
    .LoadFromMemory(YarpConfigurations.GetVerifierRoutes(),
        YarpConfigurations.GetVerifierClusters(
            builder.Configuration["SwiyuVerifierMgmtUrl"]!));
 //   .AddTransforms<SwiyuLoggingTransformProvider>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapReverseProxy();

app.UseStaticFiles();

app.Run();

// Proxy Endpoints:
// https://localhost:7009/.well-known/openid-configuration
// https://swiyu-endpoints-proxy.livelysand-4f5c661d.switzerlandnorth.azurecontainerapps.io/.well-known/openid-configuration

// https://swiyu-endpoints-proxy.livelysand-4f5c661d.switzerlandnorth.azurecontainerapps.io/issuer_openidconfigfile¨_v2.json