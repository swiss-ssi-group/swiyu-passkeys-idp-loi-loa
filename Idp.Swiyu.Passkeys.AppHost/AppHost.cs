using Aspire.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

const string IDENTITY_PROVIDER = "identityProvider";
const string WEB_CLIENT = "webClient";
const string API_SERVICE = "apiService";
const string CACHE = "cache";

const string HTTP = "http";
int VERIFIER_PORT = 80;
if (builder.ExecutionContext.IsRunMode)
{
    VERIFIER_PORT = 8084;
}

// management
IResourceBuilder<ContainerResource>? swiyuVerifier = null;
IResourceBuilder<ProjectResource>? identityProvider = null;
IResourceBuilder<ProjectResource>? swiyuProxy = null;

var sqlServer = builder.AddAzureSqlServer("sqlserver");
var database = sqlServer.AddDatabase("database", "IdpSwiyuPasskeysSts");

var migrationService = builder.AddProject<Idp_Swiyu_Passkeys_Sts_Domain_Migrations>("migrations")
    .WithReference(database)
    .WaitFor(sqlServer);

var postGresUser = builder.AddParameter("postgresuser");
var postGresPassword = builder.AddParameter("postgrespassword", secret: true);
var postGresDbIssuer = builder.AddParameter("postgresdbissuer");
var postGresJdbcIssuer = builder.AddParameter("postgresjdbcissuer");
var postGresDbVerifier = builder.AddParameter("postgresdbverifier");
var postGresJdbcVerifier = builder.AddParameter("postgresjdbcverifier");

var idpWellKnownEndpoint = builder.AddParameter("idpwellknownendpoint");
var idpJwksUri = builder.AddParameter("idpjwksuri");

var cache = builder.AddRedis(CACHE);

var issuerId = builder.AddParameter("issuerid");

// Verifier
var verifierExternalUrl = builder.AddParameter("verifierexternalurl");
var verifierOpenIdClientMetaDataFile = builder.AddParameter("verifieropenidclientmetadatafile");
var verifierDid = builder.AddParameter("verifierdid");
var didVerifierMethod = builder.AddParameter("didverifiermethod");
var verifierName = builder.AddParameter("verifiername");
var verifierSigningKey = builder.AddParameter("verifiersigningkey", true);


/////////////////////////////////////////////////////////////////
// Verifier OpenID Endpoint: Must be deployed to a public URL
/////////////////////////////////////////////////////////////////
// Verifier Management Endpoint: TODO Add JWT security verifier
// Add security to management API, disabled
// https://github.com/swiyu-admin-ch/swiyu-verifier?tab=readme-ov-file#security
/////////////////////////////////////////////////////////////////
swiyuVerifier = builder.AddContainer("swiyu-verifier", "ghcr.io/swiyu-admin-ch/swiyu-verifier", "latest")
    //.WaitFor(identityProvider)
    .WithEnvironment("EXTERNAL_URL", verifierExternalUrl)
    .WithEnvironment("OPENID_CLIENT_METADATA_FILE", verifierOpenIdClientMetaDataFile)
    .WithEnvironment("VERIFIER_DID", verifierDid)
    .WithEnvironment("DID_VERIFICATION_METHOD", didVerifierMethod)
    .WithEnvironment("SIGNING_KEY", verifierSigningKey)
    .WithEnvironment("POSTGRES_USER", postGresUser)
    .WithEnvironment("POSTGRES_PASSWORD", postGresPassword)
    .WithEnvironment("POSTGRES_DB", postGresDbVerifier)
    .WithEnvironment("POSTGRES_JDBC", postGresJdbcVerifier)
    .WithEnvironment("SPRING_SECURITY_OAUTH2_RESOURCESERVER_JWT_ISSUERURI", "https://login.microsoftonline.com/5698af84-5720-4ff0-bdc3-9d9195314244/v2.0")
    //.WithEnvironment("SPRING_SECURITY_OAUTH2_RESOURCESERVER_JWT_JWKSETURI", $"{identityProvider.GetEndpoint(HTTP)}/{idpJwksUri}")
    .WithHttpEndpoint(port: VERIFIER_PORT, targetPort: 8080, name: HTTP);

swiyuProxy = builder.AddProject<Projects.Swiyu_Endpoints_Proxy>("swiyu-endpoints-proxy")
    .WaitFor(swiyuVerifier)
    .WithEnvironment("SwiyuVerifierMgmtUrl", swiyuVerifier.GetEndpoint(HTTP))
    .WithExternalHttpEndpoints();

identityProvider = builder.AddProject<Projects.Idp_Swiyu_Passkeys_Sts>(IDENTITY_PROVIDER)
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(database)
    .WaitForCompletion(migrationService)
    .WithEnvironment("SwiyuVerifierMgmtUrl", swiyuVerifier.GetEndpoint(HTTP))
    .WithEnvironment("SwiyuOid4vpUrl", verifierExternalUrl)
    .WithEnvironment("ISSUER_ID", issuerId)
    .WaitFor(swiyuVerifier)
    .WaitFor(swiyuProxy);

var apiService = builder.AddProject<Projects.Idp_Swiyu_Passkeys_ApiService>(API_SERVICE)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Idp_Swiyu_Passkeys_Web>(WEB_CLIENT)
    .WithExternalHttpEndpoints()
    //   .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

if (builder.ExecutionContext.IsRunMode)
{
    sqlServer.RunAsContainer(container =>
    {
        container.WithDataVolume();
    });
}
else
{
    // TODO setup production SQL server configuration
}
builder.Build().Run();
