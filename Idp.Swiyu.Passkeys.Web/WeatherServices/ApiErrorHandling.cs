using System.Text;

namespace Idp.Swiyu.Passkeys.Web.WeatherServices;

public static class ApiErrorHandling
{
    public static string ParseErrorDescriptionFromResponse(HttpResponseMessage response)
    {
        var errorMeassage = new StringBuilder();
        errorMeassage.Append($"Reason: {response.ReasonPhrase}, ");

        // Get the WWW-Authenticate header
        if (response.Headers.WwwAuthenticate.Any())
        {
            foreach (var authHeader in response.Headers.WwwAuthenticate)
            {
                var headerValue = authHeader.ToString();

                errorMeassage.Append(headerValue);
            }
        }
        else
        {
            errorMeassage.Append("Unauthorized access to weather API, incorrect WwwAuthenticate header returned");
        }

            
        return errorMeassage.ToString();
    }
}