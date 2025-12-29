var builder = DistributedApplication.CreateBuilder(args);


var IDENTITY_PROVIDER = "identityProvider";
var WEB_CLIENT = "webClient";
var API_SERVICE = "apiService";
var CACHE = "cache";

const string HTTP = "http";

// public
//IResourceBuilder<ContainerResource>? swiyuOid4vp = null;

// management
IResourceBuilder<ContainerResource>? swiyuVerifierMgmt = null;
IResourceBuilder<ProjectResource>? identityProvider = null;

var postGresUser = builder.AddParameter("postgresuser");
var postGresPassword = builder.AddParameter("postgrespassword", secret: true);
var postGresDbIssuer = builder.AddParameter("postgresdbissuer");
var postGresJdbcIssuer = builder.AddParameter("postgresjdbcissuer");
var postGresDbVerifier = builder.AddParameter("postgresdbverifier");
var postGresJdbcVerifier = builder.AddParameter("postgresjdbcverifier");

var cache = builder.AddRedis(CACHE);

var issuerId = builder.AddParameter("issuerid");

// Verifier
var verifierExternalUrl = builder.AddParameter("verifierexternalurl");
var verifierOpenIdClientMetaDataFile = builder.AddParameter("verifieropenidclientmetadatafile");
var verifierDid = builder.AddParameter("verifierdid");
var didVerifierMethod = builder.AddParameter("didverifiermethod");
var verifierName = builder.AddParameter("verifiername");
var verifierSigningKey = builder.AddParameter("verifiersigningkey", true);

// Verifier: Must be deployed to a public URL
//swiyuOid4vp = builder.AddContainer("swiyu-oid4vp", "ghcr.io/swiyu-admin-ch/eidch-verifier-agent-oid4vp", "latest")
//    .WithEnvironment("EXTERNAL_URL", verifierExternalUrl)
//    .WithEnvironment("OPENID_CLIENT_METADATA_FILE", verifierOpenIdClientMetaDataFile)
//    .WithEnvironment("VERIFIER_DID", verifierDid)
//    .WithEnvironment("DID_VERIFICATION_METHOD", didVerifierMethod)
//    .WithEnvironment("VERIFIER_NAME", verifierName)
//    .WithEnvironment("SIGNING_KEY", verifierSigningKey)
//    .WithEnvironment("POSTGRES_USER", postGresUser)
//    .WithEnvironment("POSTGRES_PASSWORD", postGresPassword)
//    .WithEnvironment("POSTGRES_DB", postGresDbVerifier)
//    .WithEnvironment("POSTGRES_JDBC", postGresJdbcVerifier)
//    .WithHttpEndpoint(port: 80, targetPort: 8080, name: HTTP)
//    .WithExternalHttpEndpoints();
                                                                 // new : "ghcr.io/swiyu-admin-ch/swiyu-verifier"
swiyuVerifierMgmt = builder.AddContainer("swiyu-verifier-mgmt", "ghcr.io/swiyu-admin-ch/eidch-verifier-agent-management", "latest")
    .WithEnvironment("OID4VP_URL", verifierExternalUrl)
    .WithEnvironment("POSTGRES_USER", postGresUser)
    .WithEnvironment("POSTGRES_PASSWORD", postGresPassword)
    .WithEnvironment("POSTGRES_DB", postGresDbVerifier)
    .WithEnvironment("POSTGRES_JDBC", postGresJdbcVerifier)
#if DEBUG
      .WithHttpEndpoint(port: 8084, targetPort: 8080, name: HTTP);  // local development
#else
      .WithHttpEndpoint(port: 80, targetPort: 8080, name: HTTP); // for deployment 
#endif

identityProvider = builder.AddProject<Projects.Idp_Swiyu_Passkeys_Sts>(IDENTITY_PROVIDER)
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithEnvironment("SwiyuVerifierMgmtUrl", swiyuVerifierMgmt.GetEndpoint(HTTP))
    .WithEnvironment("SwiyuOid4vpUrl", verifierExternalUrl)
    .WithEnvironment("ISSUER_ID", issuerId)
    .WaitFor(swiyuVerifierMgmt);

var apiService = builder.AddProject<Projects.Idp_Swiyu_Passkeys_ApiService>(API_SERVICE)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Idp_Swiyu_Passkeys_Web>(WEB_CLIENT)
    .WithExternalHttpEndpoints()
 //   .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
