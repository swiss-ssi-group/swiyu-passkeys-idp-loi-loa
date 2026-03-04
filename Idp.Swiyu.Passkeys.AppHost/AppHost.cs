using Microsoft.AspNetCore.Http;
using Projects;

const string HTTP = "http";
const string IDENTITY_PROVIDER = "identity-provider";
const string WEB_CLIENT = "web-client";
const string API_SERVICE = "api-service";

var builder = DistributedApplication.CreateBuilder(args);

// management & public endpoints
IResourceBuilder<ContainerResource>? swiyuVerifier = null;
IResourceBuilder<ProjectResource>? swiyuProxy = null;
IResourceBuilder<ProjectResource>? identityProvider = null;

// E-ID database
var postGresUser = builder.AddParameter("postgresuser");
var postGresPassword = builder.AddParameter("postgrespassword", secret: true);
var postGresDbIssuer = builder.AddParameter("postgresdbissuer");

var postGresJdbcIssuer = builder.AddParameter("postgresjdbcissuer");
var postGresDbVerifier = builder.AddParameter("postgresdbverifier");
var postGresJdbcVerifier = builder.AddParameter("postgresjdbcverifier");

var stsOidcWebClientPublicPem = builder.AddParameter("StsOidcWebClientPublicPem");
var webOidcClientPrivatePem = builder.AddParameter("WebOidcClientPrivatePem", secret: true);
var webOidcClientPublicPem = builder.AddParameter("WebOidcClientPublicPem");
var webDpopClientPrivatePem = builder.AddParameter("WebDpopClientPrivatePem", secret: true);
var webDpopClientPublicPem = builder.AddParameter("WebDpopClientPublicPem");

var stsSigningPrivatePem = builder.AddParameter("StsSigningPrivatePem", secret: true);
var stsSigningPublicPem = builder.AddParameter("StsSigningPublicPem");

// Issuer
var issuerExternalUrl = builder.AddParameter("issuerexternalurl");
var issuerId = builder.AddParameter("issuerid");
var issuerDidSdJwtVerficiationMethod = builder.AddParameter("issuerdidsdjwtverificationmethod");
var issuerSdJwtKey = builder.AddParameter("issuersdjwtkey", secret: true);
var issuerOpenIdConfigFile = builder.AddParameter("issueropenidconfigfile");
var issuerMetaDataConfigFile = builder.AddParameter("issuermetadataconfigfile");
var issuerTokenTtl = builder.AddParameter("issuertokenttl");

var issuerName = builder.AddParameter("issuername");
var businessPartnerId = builder.AddParameter("businesspartnerid", secret: true);
var swiyuCustomerKey = builder.AddParameter("swiyucustomerkey", secret: true);
var swiyuCustomerSecret = builder.AddParameter("swiyucustomerSecret", secret: true);
var swiyuRefreshToken = builder.AddParameter("swiyurefreshtoken", secret: true);
var swiyuAccessToken = builder.AddParameter("swiyuaccesstoken", secret: true);

// Verifier
var verifierExternalUrl = builder.AddParameter("verifierexternalurl");
var verifierOpenIdClientMetaDataFile = builder.AddParameter("verifieropenidclientmetadatafile");
var verifierDid = builder.AddParameter("verifierdid");
var didVerifierMethod = builder.AddParameter("didverifiermethod");
var verifierName = builder.AddParameter("verifiername");
var verifierSigningKey = builder.AddParameter("verifiersigningkey", true);

var idpWellKnownEndpoint = builder.AddParameter("idpwellknownendpoint");
var idpJwksUri = builder.AddParameter("idpjwksuri");
var verifierJwtIssuer = builder.AddParameter("verifierjwtissuer");

/////////////////////////////////////////////////////////////////
// Verifier OpenID Endpoint: Must be deployed to a public URL
/////////////////////////////////////////////////////////////////
// Verifier Management Endpoint: TODO Add JWT security verifier
// Add security to management API, disabled
// https://github.com/swiyu-admin-ch/swiyu-verifier?tab=readme-ov-file#security
/////////////////////////////////////////////////////////////////
swiyuVerifier = builder.AddContainer("swiyu-verifier", "ghcr.io/swiyu-admin-ch/swiyu-verifier", "latest")
    .WithEnvironment("EXTERNAL_URL", verifierExternalUrl)
    .WithEnvironment("OPENID_CLIENT_METADATA_FILE", verifierOpenIdClientMetaDataFile)
    .WithEnvironment("VERIFIER_DID", verifierDid)
    .WithEnvironment("DID_VERIFICATION_METHOD", didVerifierMethod)
    .WithEnvironment("SIGNING_KEY", verifierSigningKey)
    .WithEnvironment("POSTGRES_USER", postGresUser)
    .WithEnvironment("POSTGRES_PASSWORD", postGresPassword)
    .WithEnvironment("POSTGRES_DB", postGresDbVerifier)
    .WithEnvironment("POSTGRES_JDBC", postGresJdbcVerifier)
    .WithEnvironment("SPRING_SECURITY_OAUTH2_RESOURCESERVER_JWT_ISSUERURI", verifierJwtIssuer)
    //.WithHttpEndpoint(port: 8084, targetPort: 8080, name: HTTP);  // local development
    .WithHttpEndpoint(port: 80, targetPort: 8080, name: HTTP); // for deployment 

var sqlServer = builder.AddAzureSqlServer("sqlserver");
var database = sqlServer.AddDatabase("database", "IdpSwiyuPasskeysSts");

var migrationService = builder.AddProject<Idp_Swiyu_Passkeys_Sts_Domain_Migrations>("migrations")
    .WithReference(database)
    .WaitFor(sqlServer);

swiyuProxy = builder.AddProject<Projects.Swiyu_Endpoints_Proxy>("swiyu-endpoints-proxy")
    .WaitFor(swiyuVerifier)
    .WithEnvironment("SwiyuVerifierMgmtUrl", swiyuVerifier.GetEndpoint(HTTP))
    .WithExternalHttpEndpoints();

var swiyuManagementClientId = builder.AddParameter("swiyumanagementclientid");
var swiyuManagementClientSecret = builder.AddParameter("swiyumanagementclientsecret", true);
var swiyuManagementAuthority = builder.AddParameter("swiyumanagementauthority");
var swiyuManagementScope = builder.AddParameter("swiyumanagementscope");
var webClientUrl = builder.AddParameter("WebClientUrl");

// OIDC web endpoints
var webOidcClientId = builder.AddParameter("WebOidcClientId");
var webOidcAuthority = builder.AddParameter("WebOidcAuthority");

identityProvider = builder.AddProject<Projects.Idp_Swiyu_Passkeys_Sts>(IDENTITY_PROVIDER)
    .WithExternalHttpEndpoints()
    .WithReference(database)
    .WaitForCompletion(migrationService)
    .WithEnvironment("SwiyuVerifierMgmtUrl", swiyuVerifier.GetEndpoint(HTTP))
    .WithEnvironment("SwiyuOid4vpUrl", verifierExternalUrl)
    .WithEnvironment("ISSUER_ID", issuerId)
    .WithEnvironment("SwiyuManagementClientId", swiyuManagementClientId)
    .WithEnvironment("SwiyuManagementClientSecret", swiyuManagementClientSecret)
    .WithEnvironment("SwiyuManagementAuthority", swiyuManagementAuthority)
    .WithEnvironment("SwiyuManagementScope", swiyuManagementScope)
    .WithEnvironment("WebClientUrl", webClientUrl)
    .WithEnvironment("WebOidcAuthority", webOidcAuthority)
    .WithEnvironment("StsOidcWebClientPublicPem", stsOidcWebClientPublicPem)
    .WithEnvironment("StsSigningPrivatePem", stsSigningPrivatePem)
    .WithEnvironment("StsSigningPublicPem", stsSigningPublicPem)
    .WaitFor(swiyuVerifier)
    .WaitFor(swiyuProxy)
    .WithHttpHealthCheck("/health");

var apiService = builder.AddProject<Projects.Idp_Swiyu_Passkeys_ApiService>(API_SERVICE)
    .WithReference(identityProvider)
    .WaitFor(identityProvider)
    .WithHttpHealthCheck("/health")
    .WithEnvironment("WebOidcAuthority", webOidcAuthority);

builder.AddProject<Projects.Idp_Swiyu_Passkeys_Web>(WEB_CLIENT)
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("WebOidcAuthority", webOidcAuthority)
    .WithEnvironment("WebOidcClientId", webOidcClientId)
    .WithEnvironment("WebOidcClientPrivatePem", webOidcClientPrivatePem)
    .WithEnvironment("WebOidcClientPublicPem", webOidcClientPublicPem)
    .WithEnvironment("WebDpopClientPrivatePem", webDpopClientPrivatePem)
    .WithEnvironment("WebDpopClientPublicPem", webDpopClientPublicPem)
    .WithHttpHealthCheck("/health")
    .WaitFor(identityProvider)
    .WithReference(identityProvider);

if (builder.ExecutionContext.IsRunMode)
{
    sqlServer.RunAsContainer(container =>
    {
        container.WithDataVolume();
    });
}

builder.Build().Run();

