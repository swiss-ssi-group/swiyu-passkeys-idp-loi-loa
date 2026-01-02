using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Idp.Swiyu.Passkeys.Web;

public class WeatherApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    public WeatherApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("dpop-api-client");
        
        HttpResponseMessage? response = null;
        try
        {
            // Make a direct request to check for 401 first
            response = await httpClient.GetAsync("/weatherforecast", cancellationToken);
            
            // Check if we got a 401 response
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Parse the WWW-Authenticate header to extract error_description
                var errorDescription = ParseErrorDescriptionFromResponse(response);
                throw new WeatherApiException(errorDescription ?? "Unauthorized access to weather API.");
            }
            
            // Ensure success status code
            response.EnsureSuccessStatusCode();
            
            // Read the response as an array
            var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>(cancellationToken);
            
            // Take only maxItems
            if (forecasts != null && forecasts.Length > maxItems)
            {
                return forecasts.Take(maxItems).ToArray();
            }
            
            return forecasts ?? [];
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Fallback for any other 401 errors
            var errorDescription = ParseErrorDescriptionFromException(ex);
            throw new WeatherApiException(errorDescription ?? "Unauthorized access to weather API.", ex);
        }
        finally
        {
            response?.Dispose();
        }
    }

    private static string? ParseErrorDescriptionFromResponse(HttpResponseMessage response)
    {
        // Get the WWW-Authenticate header
        if (response.Headers.WwwAuthenticate.Any())
        {
            foreach (var authHeader in response.Headers.WwwAuthenticate)
            {
                var headerValue = authHeader.ToString();
                
                // Try to extract error_description using regex
                var match = Regex.Match(headerValue, @"error_description=""([^""]+)""");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }

        return null;
    }

    private static string? ParseErrorDescriptionFromException(HttpRequestException ex)
    {
        // The error details might be in the message
        var message = ex.Message;
        
        // Try to extract error_description using regex
        var match = Regex.Match(message, @"error_description=""([^""]+)""");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }
}

public class WeatherApiException : Exception
{
    public WeatherApiException(string message) : base(message)
    {
    }

    public WeatherApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
