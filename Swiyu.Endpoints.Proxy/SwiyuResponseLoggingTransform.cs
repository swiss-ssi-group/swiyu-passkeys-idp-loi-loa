using System.Text;
using Yarp.ReverseProxy.Transforms;

namespace Swiyu.Endpoints.Proxy;

public class SwiyuResponseLoggingTransform : ResponseTransform
{
    private readonly ILogger<SwiyuResponseLoggingTransform> _logger;
    private readonly string _routeName;

    public SwiyuResponseLoggingTransform(ILogger<SwiyuResponseLoggingTransform> logger, string routeName)
    {
        _logger = logger;
        _routeName = routeName;
    }

    public override async ValueTask ApplyAsync(ResponseTransformContext context)
    {
        var response = context.HttpContext.Response;

        _logger.LogInformation("[{RouteName}] Response: StatusCode={StatusCode}", 
            _routeName, 
            response.StatusCode);

        // Log response headers (optional)
        foreach (var header in response.Headers.Where(h => 
            h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) || 
            h.Key.Equals("Location", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogInformation("[{RouteName}] Response Header: {Key}={Value}", 
                _routeName, 
                header.Key, 
                header.Value);
        }

        // Note: Reading response body in YARP transforms is complex and can affect performance
        // If you need to log response bodies, consider using middleware instead
    }
}
