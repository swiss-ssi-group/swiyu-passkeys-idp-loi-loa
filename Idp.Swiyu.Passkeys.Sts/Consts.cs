namespace Idp.Swiyu.Passkeys.Sts;

public static class Consts
{
    // loa (Level of Authentication)
    public const string LOA = "loa";

    //passkeys, (public/private key certificate authentication)
    public const string LOA_400 = "loa.400";

    // authenticator apps, OpenID verifiable credentials(E-ID, swiyu)
    public const string LOA_300 = "loa.300";

    // SMS, email, TOTP, 2-step
    public const string LOA_200 = "loa.200";

    // single factor, SAS key, API Keys, passwords, OTP
    public const string LOA_100 = "loa.100";

    // loi (Level of Identification)
    public const string LOI = "loi";

    // Offline Human identification by trusted official in trustworthy organisation.
    public const string LOI_500 = "loi.500";

    // OpenID verifiable credentials (E-ID, swiyu), government issued.
    public const string LOI_400 = "loi.400";

    // Digital online check with person
    public const string LOI_300 = "loi.300";

    // Digital video without person
    public const string LOI_200 = "loi.200";

    // Email & SMS validation, or none
    public const string LOI_100 = "loi.100";
}
