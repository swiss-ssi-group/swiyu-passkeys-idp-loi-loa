using System.Text;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Swiyu.Endpoints.Proxy;

public class SwiyuLoggingTransform : RequestTransform
{
    private readonly ILogger<SwiyuLoggingTransform> _logger;
    private readonly string _routeName;

    public SwiyuLoggingTransform(ILogger<SwiyuLoggingTransform> logger, string routeName)
    {
        _logger = logger;
        _routeName = routeName;
    }

    public override async ValueTask ApplyAsync(RequestTransformContext context)
    {
        var request = context.HttpContext.Request;

        _logger.LogInformation("[{RouteName}] Request: {Method} {Path}{QueryString}", 
            _routeName, 
            request.Method, 
            request.Path, 
            request.QueryString);

        // Log query parameters
        if (request.Query.Any())
        {
            foreach (var param in request.Query)
            {
                _logger.LogInformation("[{RouteName}] Query Parameter: {Key}={Value}", 
                    _routeName, 
                    param.Key, 
                    param.Value);
            }
        }

        // Log form data for POST requests
        if (request.Method == "POST" && request.HasFormContentType)
        {
            try
            {
                var form = await request.ReadFormAsync();
                foreach (var field in form)
                {
                    var value = field.Value.ToString();

                    // Special handling for vp_token to check for SD-JWT format
                    if (field.Key == "vp_token")
                    {
                        _logger.LogInformation("[{RouteName}] vp_token={Token}", _routeName, value);
                        _logger.LogInformation("[{RouteName}] ContainsTilde={Contains}", _routeName, value.Contains("~"));

                        // Additional SD-JWT format validation logging
                        var tildeCount = value.Count(c => c == '~');
                        _logger.LogInformation("[{RouteName}] TildeCount={Count}", _routeName, tildeCount);

                        if (!value.Contains("~"))
                        {
                            _logger.LogWarning("[{RouteName}] Invalid SD-JWT format: missing tilde separator", _routeName);
                        }
                    }
                    else
                    {
                        // Log other form fields (potentially truncate long values)
                        var logValue = value.Length > 200 ? value.Substring(0, 200) + "..." : value;
                        _logger.LogInformation("[{RouteName}] Form Field: {Key}={Value}", 
                            _routeName, 
                            field.Key, 
                            logValue);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{RouteName}] Error reading form data", _routeName);
            }
        }
        // Log request body for JSON content
        else if (request.ContentLength > 0 && 
                 request.ContentType?.Contains("application/json") == true)
        {
            try
            {
                request.EnableBuffering();
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;

                var logBody = body.Length > 500 ? body.Substring(0, 500) + "..." : body;
                _logger.LogInformation("[{RouteName}] Request Body: {Body}", _routeName, logBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{RouteName}] Error reading request body", _routeName);
            }
        }

        // Log headers (optional - uncomment if needed)
        // foreach (var header in request.Headers.Where(h => !h.Key.StartsWith("Authorization")))
        // {
        //     _logger.LogDebug("[{RouteName}] Header: {Key}={Value}", _routeName, header.Key, header.Value);
        // }
    }
}

public class SwiyuLoggingTransformProvider : ITransformProvider
{
    private readonly ILogger<SwiyuLoggingTransform> _requestLogger;
    private readonly ILogger<SwiyuResponseLoggingTransform> _responseLogger;

    public SwiyuLoggingTransformProvider(
        ILogger<SwiyuLoggingTransform> requestLogger,
        ILogger<SwiyuResponseLoggingTransform> responseLogger)
    {
        _requestLogger = requestLogger;
        _responseLogger = responseLogger;
    }

    public void ValidateRoute(TransformRouteValidationContext context)
    {
        // No validation needed
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
        // No validation needed
    }

    public void Apply(TransformBuilderContext context)
    {
        // Apply logging only to oid4vp and oid4vci routes
        var routeId = context.Route.RouteId;

        if (routeId == "routeverifier" || routeId == "routeissuer")
        {
            var routeName = routeId == "routeverifier" ? "oid4vp" : "oid4vci";

            // Add request logging
            context.AddRequestTransform(transformContext => 
            {
                var transform = new SwiyuLoggingTransform(_requestLogger, routeName);
                return transform.ApplyAsync(transformContext);
            });

            // Add response logging
            context.AddResponseTransform(transformContext =>
            {
                var transform = new SwiyuResponseLoggingTransform(_responseLogger, routeName);
                return transform.ApplyAsync(transformContext);
            });
        }
    }
}
