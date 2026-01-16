using Swiyu.Endpoints.Proxy;
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromMemory(YarpConfigurations.GetRoutes(), 
        YarpConfigurations.GetClusters(builder.Configuration["SwiyuIssuerMgmtUrl"]!, 
            builder.Configuration["SwiyuVerifierMgmtUrl"]!));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapReverseProxy();

app.UseStaticFiles();

app.Run();

// Proxy Endpoints:
// https://localhost:7009/.well-known/openid-configuration
// https://swiyu-endpoints-proxy.redpebble-62dbc6b1.switzerlandnorth.azurecontainerapps.io/.well-known/openid-configuration

// https://swiyu-endpoints-proxy.redpebble-62dbc6b1.switzerlandnorth.azurecontainerapps.io/issuer_openidconfigfile.json