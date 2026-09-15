using System.Text;

namespace ConvertCertiticatesBase64;

public static class ConfigConverter
{
    public static string GetPemFromBase64(string base64String)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
    }

    public static string CreateBase64FromPem(string pem)
    {
        var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(pem));

        return base64String;
    }
}
