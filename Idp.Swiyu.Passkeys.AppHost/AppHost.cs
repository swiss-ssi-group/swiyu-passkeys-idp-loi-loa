using Microsoft.AspNetCore.Http;
using Projects;
using System.Text;

const string HTTP = "http";
const string IDENTITY_PROVIDER = "identity-provider";
const string WEB_CLIENT = "web-client";
const string API_SERVICE = "api-service";

var builder = DistributedApplication.CreateBuilder(args);

// management & public endpoints
IResourceBuilder<ContainerResource>? swiyuVerifier = null;
IResourceBuilder<ProjectResource>? swiyuProxy = null;
IResourceBuilder<ProjectResource>? identityProvider = null;
IResourceBuilder<ContainerResource>? swiyuIssuer = null;

// E-ID database
var POSTGRES_USER = builder.AddParameter("postgresuser");
var POSTGRES_PASSWORD = builder.AddParameter("postgrespassword", secret: true);
var ISSUER_POSTGRES_DB = builder.AddParameter("postgresdbissuer");
var ISSUER_POSTGRES_JDBC = builder.AddParameter("postgresjdbcissuer");

var VERIFIER_POSTGRES_DB = builder.AddParameter("postgresdbverifier");
var VERIFIER_POSTGRES_JDBC = builder.AddParameter("postgresjdbcverifier");

var stsOidcWebClientPublicPemBase64 = builder.AddParameter("StsOidcWebClientPublicPemBase64");
var webOidcClientPrivatePemBase64 = builder.AddParameter("WebOidcClientPrivatePemBase64", secret: true);
var webOidcClientPublicPemBase64 = builder.AddParameter("WebOidcClientPublicPemBase64");
var webDpopClientPrivatePemBase64 = builder.AddParameter("WebDpopClientPrivatePemBase64", secret: true);
var webDpopClientPublicPemBase64 = builder.AddParameter("WebDpopClientPublicPemBase64");

var stsSigningPrivatePemBase64 = builder.AddParameter("StsSigningPrivatePemBase64", secret: true);
var stsSigningPublicPemBase64 = builder.AddParameter("StsSigningPublicPemBase64");

// Issuer
var ISSUER_EXTERNAL_URL = builder.AddParameter("issuerexternalurl");
var ISSUER_ID = builder.AddParameter("issuerid");
var ISSUER_DID_SDJWT_VERIFICATION_METHOD = builder.AddParameter("issuerdidsdjwtverificationmethod");

var ISSUER_SDJWT_KEY_BASE64 = builder.AddParameter("issuersdjwtkeybase64", secret: true);
var issuerSdJwtKeyBase64Value = await ISSUER_SDJWT_KEY_BASE64.Resource.GetValueAsync(default);
var ISSUER_SDJWT_KEY = Encoding.UTF8.GetString(Convert.FromBase64String((issuerSdJwtKeyBase64Value ?? string.Empty)));

var ISSUER_OPENID_CONFIG_FILE = builder.AddParameter("issueropenidconfigfile");
var ISSUER_METADATA_CONFIG_FILE = builder.AddParameter("issuermetadataconfigfile");
var TOKEN_TTL = builder.AddParameter("issuertokenttl");

var ISSUER_SPRING_APPLICATION_NAME = builder.AddParameter("issuername");
var SWIYU_PARTNER_ID = builder.AddParameter("businesspartnerid", secret: true);
var SWIYU_STATUS_REGISTRY_CUSTOMER_KEY = builder.AddParameter("swiyucustomerkey", secret: true);
var SWIYU_STATUS_REGISTRY_CUSTOMER_SECRET = builder.AddParameter("swiyucustomersecret", secret: true);

// Verifier
var VERIFIER_EXTERNAL_URL = builder.AddParameter("verifierexternalurl");
var VERIFIER_OPENID_CLIENT_METADATA_FILE = builder.AddParameter("verifieropenidclientmetadatafile");
var VERIFIER_DID = builder.AddParameter("verifierdid");
var DID_VERIFICATION_METHOD = builder.AddParameter("didverifiermethod");
var VERIFIER_NAME = builder.AddParameter("verifiername");

var VERIFIER_SIGNING_KEY_BASE64 = builder.AddParameter("verifiersigningkeybase64", secret: true);
var verifierSigningKeyBase64Value = await VERIFIER_SIGNING_KEY_BASE64.Resource.GetValueAsync(default);
var VERIFIER_SIGNING_KEY = Encoding.UTF8.GetString(Convert.FromBase64String((verifierSigningKeyBase64Value ?? string.Empty)));

var IDP_WELL_KNOWN_ENDPOINT = builder.AddParameter("idpwellknownendpoint");
var IDP_JWKS_URI = builder.AddParameter("idpjwksuri");
var SPRING_SECURITY_OAUTH2_RESOURCESERVER_JWT_ISSUERURI = builder.AddParameter("verifierjwtissuer");


/////////////////////////////////////////////////////////////////
// Verifier OpenID Endpoint: Must be deployed to a public URL
/////////////////////////////////////////////////////////////////
// https://github.com/swiyu-admin-ch/swiyu-verifier?tab=readme-ov-file#security
/////////////////////////////////////////////////////////////////
swiyuVerifier = builder.AddContainer("swiyu-verifier", "ghcr.io/swiyu-admin-ch/swiyu-verifier", "5.0.1")
    .WithEnvironment("EXTERNAL_URL", VERIFIER_EXTERNAL_URL)
    .WithEnvironment("OPENID_CLIENT_METADATA_FILE", VERIFIER_OPENID_CLIENT_METADATA_FILE)
    .WithEnvironment("VERIFIER_DID", VERIFIER_DID)
    .WithEnvironment("DID_VERIFICATION_METHOD", DID_VERIFICATION_METHOD)
    .WithEnvironment("SIGNING_KEY", VERIFIER_SIGNING_KEY)
    .WithEnvironment("POSTGRES_USER", POSTGRES_USER)
    .WithEnvironment("POSTGRES_PASSWORD", POSTGRES_PASSWORD)
    .WithEnvironment("POSTGRES_DB", VERIFIER_POSTGRES_DB)
    .WithEnvironment("POSTGRES_JDBC", VERIFIER_POSTGRES_JDBC)
    .WithEnvironment("SPRING_SECURITY_OAUTH2_RESOURCESERVER_JWT_ISSUERURI", SPRING_SECURITY_OAUTH2_RESOURCESERVER_JWT_ISSUERURI)
    .WithEnvironment("VERIFICATION_EXPIRY_MUST_BE_PRESENT", "false")
    //.WithHttpEndpoint(port: 8084, targetPort: 8080, name: HTTP);  // local development
    .WithHttpEndpoint(port: 80, targetPort: 8080, name: HTTP); // for deployment 
    // Testing only, not required for IDP

// Issuer
var SWIYU_STATUS_REGISTRY_BOOTSTRAP_REFRESH_TOKEN = builder.AddParameter("swiyurefreshtoken", secret: true);
var SWIYU_STATUS_REGISTRY_ACCESS_TOKEN = builder.AddParameter("swiyuaccesstoken", secret: true);

/////////////////////////////////////////////////////////////////
// Issuer OpenID Endpoint: Must be deployed to a public URL
/////////////////////////////////////////////////////////////////
// Issuer Management Endpoint: TODO Add JWT security issuer
// Add security to management API, disabled
// https://github.com/swiyu-admin-ch/swiyu-issuer?tab=readme-ov-file#security
/////////////////////////////////////////////////////////////////
swiyuIssuer = builder.AddContainer("swiyu-issuer", "ghcr.io/swiyu-admin-ch/swiyu-issuer", "5.0.1")
    .WithEnvironment("EXTERNAL_URL", ISSUER_EXTERNAL_URL)
    .WithEnvironment("SPRING_APPLICATION_NAME", ISSUER_SPRING_APPLICATION_NAME)
    .WithEnvironment("ISSUER_ID", ISSUER_ID)

    .WithEnvironment("DID_STATUS_LIST_VERIFICATION_METHOD", ISSUER_DID_SDJWT_VERIFICATION_METHOD)
    .WithEnvironment("STATUS_LIST_KEY", ISSUER_SDJWT_KEY)
    .WithEnvironment("SWIYU_PARTNER_ID", SWIYU_PARTNER_ID)
    .WithEnvironment("SWIYU_STATUS_REGISTRY_CUSTOMER_KEY", SWIYU_STATUS_REGISTRY_CUSTOMER_KEY)
    .WithEnvironment("SWIYU_STATUS_REGISTRY_CUSTOMER_SECRET", SWIYU_STATUS_REGISTRY_CUSTOMER_SECRET)

    .WithEnvironment("SWIYU_STATUS_REGISTRY_ACCESS_TOKEN", SWIYU_STATUS_REGISTRY_ACCESS_TOKEN)
    .WithEnvironment("SWIYU_STATUS_REGISTRY_BOOTSTRAP_REFRESH_TOKEN", SWIYU_STATUS_REGISTRY_BOOTSTRAP_REFRESH_TOKEN)
    .WithEnvironment("SWIYU_STATUS_REGISTRY_TOKEN_URL", "https://keymanager-prd.api.admin.ch/keycloak/realms/APIGW/protocol/openid-connect/token")

    .WithEnvironment("DID_SDJWT_VERIFICATION_METHOD", ISSUER_DID_SDJWT_VERIFICATION_METHOD)
    .WithEnvironment("SDJWT_KEY", ISSUER_SDJWT_KEY)
    .WithEnvironment("OPENID_CONFIG_FILE", ISSUER_OPENID_CONFIG_FILE)
    .WithEnvironment("METADATA_CONFIG_FILE", ISSUER_METADATA_CONFIG_FILE)
    .WithEnvironment("TOKEN_TTL", TOKEN_TTL)

    .WithEnvironment("SWIYU_STATUS_REGISTRY_API_URL", "https://status-reg-api.trust-infra.swiyu-int.admin.ch")
    .WithEnvironment("LOGGING_LEVEL_CH_ADMIN_BJ_SWIYU", "DEBUG")
    .WithEnvironment("SWIYU_STATUS_REGISTRY_AUTH_ENABLE_REFRESH_TOKEN_FLOW", "true")
    .WithEnvironment("SPRING_SECURITY_OAUTH2_RESOURCESERVER_JWT_ISSUERURI", SPRING_SECURITY_OAUTH2_RESOURCESERVER_JWT_ISSUERURI)

    .WithEnvironment("POSTGRES_USER", POSTGRES_USER)
    .WithEnvironment("POSTGRES_PASSWORD", POSTGRES_PASSWORD)
    .WithEnvironment("POSTGRES_DB", ISSUER_POSTGRES_DB)
    .WithEnvironment("POSTGRES_JDBC", ISSUER_POSTGRES_JDBC)
    //.WithHttpEndpoint(port: 8082, targetPort: 8080, name: HTTP); // local development
    .WithHttpEndpoint(port: 80, targetPort: 8080, name: HTTP); // for deployment

var sqlServer = builder.AddAzureSqlServer("sqlserver")
    .ConfigureInfrastructure(infra =>
    {
        var resources = infra.GetProvisionableResources();

        var dbRes = resources.OfType<Azure.Provisioning.Sql.SqlDatabase>().Single();
        dbRes.Sku = new Azure.Provisioning.Sql.SqlSku()
        {
            Tier = "Basic",
            Name = "Basic",
            Capacity = 5
        };
        dbRes.UseFreeLimit = false;
    });

var database = sqlServer.AddDatabase("database", "IdpSwiyuPasskeysSts");

var migrationService = builder.AddProject<Idp_Swiyu_Passkeys_Sts_Domain_Migrations>("migrations")
    .WithReference(database)
    .WaitFor(sqlServer);

swiyuProxy = builder.AddProject<Projects.Swiyu_Endpoints_Proxy>("swiyu-endpoints-proxy")
    .WaitFor(swiyuVerifier)
    .WithEnvironment("SwiyuVerifierMgmtUrl", swiyuVerifier.GetEndpoint(HTTP))
    .WithEnvironment("SwiyuIssuerMgmtUrl", swiyuIssuer.GetEndpoint(HTTP))
    .WithExternalHttpEndpoints();

var swiyuManagementClientId = builder.AddParameter("SwiyuManagementClientId");
var swiyuManagementClientSecretEntra = builder.AddParameter("SwiyuManagementClientSecretEntra", true);
var swiyuManagementAuthority = builder.AddParameter("SwiyuManagementAuthority");
var swiyuManagementScope = builder.AddParameter("SwiyuManagementScope");
var webClientUrl = builder.AddParameter("WebClientUrl");

// OIDC web endpoints
var webOidcClientId = builder.AddParameter("WebOidcClientId");
var webOidcAuthority = builder.AddParameter("WebOidcAuthority");

identityProvider = builder.AddProject<Projects.Idp_Swiyu_Passkeys_Sts>(IDENTITY_PROVIDER)
    .WithExternalHttpEndpoints()
    .WithReference(database)
    .WaitForCompletion(migrationService)
    .WithEnvironment("SwiyuVerifierMgmtUrl", swiyuVerifier.GetEndpoint(HTTP))
    .WithEnvironment("SwiyuOid4vpUrl", VERIFIER_EXTERNAL_URL)
    .WithEnvironment("ISSUER_ID", ISSUER_ID)
    .WithEnvironment("SwiyuManagementClientId", swiyuManagementClientId)
    .WithEnvironment("SwiyuManagementClientSecretEntra", swiyuManagementClientSecretEntra)
    .WithEnvironment("SwiyuManagementAuthority", swiyuManagementAuthority)
    .WithEnvironment("SwiyuManagementScope", swiyuManagementScope)
    .WithEnvironment("WebClientUrl", webClientUrl)
    .WithEnvironment("WebOidcAuthority", webOidcAuthority)
    .WithEnvironment("StsOidcWebClientPublicPemBase64", stsOidcWebClientPublicPemBase64)
    .WithEnvironment("StsSigningPrivatePemBase64", stsSigningPrivatePemBase64)
    .WithEnvironment("StsSigningPublicPemBase64", stsSigningPublicPemBase64)
    .WaitFor(swiyuVerifier)
    .WaitFor(swiyuProxy)
    .WithHttpHealthCheck("/health");

var apiService = builder.AddProject<Idp_Swiyu_Passkeys_ApiService>(API_SERVICE)
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
    .WithEnvironment("WebOidcClientPrivatePemBase64", webOidcClientPrivatePemBase64)
    .WithEnvironment("WebOidcClientPublicPemBase64", webOidcClientPublicPemBase64)
    .WithEnvironment("WebDpopClientPrivatePemBase64", webDpopClientPrivatePemBase64)
    .WithEnvironment("WebDpopClientPublicPemBase64", webDpopClientPublicPemBase64)
    .WithHttpHealthCheck("/health")
    .WaitFor(identityProvider)
    .WithReference(identityProvider);

if (builder.ExecutionContext.IsRunMode)
{
    sqlServer.RunAsContainer(container =>
    {
        container.WithDataVolume();
        container.WithHostPort(50001);
    });
}

builder.Build().Run();

