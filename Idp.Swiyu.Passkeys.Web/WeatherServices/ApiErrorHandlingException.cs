namespace Idp.Swiyu.Passkeys.Web.WeatherServices;

public class ApiErrorHandlingException : Exception
{
    public ApiErrorHandlingException(string message) : base(message)
    {
    }

    public ApiErrorHandlingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
