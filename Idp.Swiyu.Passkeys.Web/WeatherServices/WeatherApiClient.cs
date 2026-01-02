using System.Net;

namespace Idp.Swiyu.Passkeys.Web.WeatherServices;

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
                var errorMessage = ApiErrorHandling.ParseErrorDescriptionFromResponse(response);
                throw new ApiErrorHandlingException(errorMessage);
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
        finally
        {
            response?.Dispose();
        }
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
