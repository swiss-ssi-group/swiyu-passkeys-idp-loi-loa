# swiyu-passkeys-idp-loi-loa

## loa (Level of Authentication)

loa.400 : passkeys, (public/private key certificate authentication)
loa.300 : authenticator apps, OpenID verifiable credentials (E-ID, swiyu)
loa.200 : SMS, email, TOTP, 2-step
loa.100 : single factor, SAS key, API Keys, passwords, OTP

## loi (Level of Identification)

loi.500 : Offline Human identification by trusted official in trustworthy organisation.
loi.400 : OpenID verifiable credentials (E-ID, swiyu), government issued.
loi.300 : Digital online check with person
loi.200 : Digital video without person
loi.100 : Email & SMS validation

## Links

https://github.com/dotnet/aspnetcore/issues/64881

https://openid.net/specs/openid-connect-eap-acr-values-1_0-final.html

https://datatracker.ietf.org/doc/html/rfc8176

https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims

https://damienbod.com/2025/12/20/digital-authentication-and-identity-validation/

https://damienbod.com/2025/07/02/implement-asp-net-core-openid-connect-with-keykloak-to-implement-level-of-authentication-loa-requirements/
