using System.Security.Authentication;
using Yarp.ReverseProxy.Configuration;

namespace Swiyu.Endpoints.Proxy;

public static class YarpConfigurations
{
    public static RouteConfig[] GetRoutes()
    {
        var routes = new[]
        {
            new RouteConfig()
            {
                RouteId = "routeissuer",
                ClusterId = "clusterissuer",
                AuthorizationPolicy = "Anonymous",
                Match = new RouteMatch
                {
                    Path = "/oid4vci/{**catch-all}"
                }
            },
            new RouteConfig()
            {
                RouteId = "routeissuerwellknown",
                ClusterId = "clusterissuer",
                AuthorizationPolicy = "Anonymous",
                Match = new RouteMatch
                {
                    Path = "/.well-known/{**catch-all}"
                }
            },
            new RouteConfig()
            {
                RouteId = "routeverifier",
                ClusterId = "clusterverifier",
                AuthorizationPolicy = "Anonymous",
                Match = new RouteMatch
                {
                    Path = "/oid4vp/{**catch-all}"
                }
            }
         };

        return routes;
    }

    public static ClusterConfig[] GetClusters(string issuer, string verifier)
    {
        var clusters = new[]
        {
            new ClusterConfig()
            {
                ClusterId = "clusterissuer",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination1", new DestinationConfig() { Address = $"{issuer}/" } }
                },
                HttpClient = new HttpClientConfig { MaxConnectionsPerServer = 10, SslProtocols =  SslProtocols.Tls12 }
            },
            new ClusterConfig()
            {
                ClusterId = "clusterverifier",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination1", new DestinationConfig() { Address = $"{verifier}/" } }
                },
                HttpClient = new HttpClientConfig { MaxConnectionsPerServer = 10, SslProtocols =  SslProtocols.Tls12 }
            }
        };

        return clusters;
    }
}
