var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var identityProvider = builder.AddProject<Projects.Idp_Swiyu_Passkeys_Sts>("idp")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache);


var apiService = builder.AddProject<Projects.Idp_Swiyu_Passkeys_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Idp_Swiyu_Passkeys_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
