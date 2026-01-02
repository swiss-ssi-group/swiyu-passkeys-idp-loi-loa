using System.Text;

namespace Idp.Swiyu.Passkeys.Web.WeatherServices;

public static class ApiErrorHandling
{
    public static string ParseErrorDescriptionFromResponse(HttpResponseMessage response)
    {
        var errorMessage = new StringBuilder();
        errorMessage.Append($"Reason: {response.ReasonPhrase}, ");

        // Get the WWW-Authenticate header
        if (response.Headers.WwwAuthenticate.Any())
        {
            foreach (var authHeader in response.Headers.WwwAuthenticate)
            {
                var headerValue = authHeader.ToString();

                errorMessage.Append(headerValue);
            }
        }
        else
        {
            errorMessage.Append("Unauthorized access to API, WWW-Authenticate header not set");
        }

            
        return errorMessage.ToString();
    }
}