# Implement swiyu identification & passkeys authentication using Duende IDP 

Example fo Aspire implementing Level of Identification (LoI) and Level of Authentication (LoA) using swiyu passkeys authentication in Duende IdentityServer with ASP.NET Core Identity.

## Blogs
- [Digital authentication and identity validation](https://damienbod.com/2025/12/20/digital-authentication-and-identity-validation/)
- [Set the amr claim when using passkeys authentication in ASP.NET Core](https://damienbod.com)
- [Planned: Implementing Level of Authentication (LoA) with ASP.NET Core Identity and Duende](https://damienbod.com)
- [Planned: Implementing Level of Identification (LoI) with ASP.NET Core Identity and Duende](https://damienbod.com)
- [Planned: Force step authentication for incorrect level](https://damienbod.com)

## loa (Level of Authentication)
- loa.400 : passkeys, (public/private key certificate authentication)
- loa.300 : authenticator apps, OpenID verifiable credentials (E-ID, swiyu)
- loa.200 : SMS, email, TOTP, 2-step
- loa.100 : single factor, SAS key, API Keys, passwords, OTP

![loa](https://github.com/swiss-ssi-group/swiyu-passkeys-idp-loi-loa/blob/main/images/authentication-levels.drawio.png)

## loi (Level of Identification)
- loi.500 : Offline Human identification by trusted official in trustworthy organisation.
- loi.400 : OpenID verifiable credentials (E-ID, swiyu), government issued.
- loi.300 : Digital online check with person
- loi.200 : Digital video without person
- loi.100 : Email & SMS validation

![loi](https://github.com/swiss-ssi-group/swiyu-passkeys-idp-loi-loa/blob/main/images/identity-levels.drawio.png)

## Architecture

The solution uses a web application which authenticates using OpenID Connect, OAuth PAR, OAuth DPoP. The IDP is implemented using Duende and ASP.NET Core Identity. When the user authenticates, passkeys are used for the user authentication. The server returns claims to the client application and the amr claim is returned with the "pop" value.

![architecture](https://github.com/swiss-ssi-group/swiyu-passkeys-idp-loi-loa/blob/main/images/OIDC_LOI_LOA.drawio.png)

## Podman

https://podman-desktop.io/docs/troubleshooting/troubleshooting-podman

## Links

https://github.com/dotnet/aspnetcore/issues/64881

https://openid.net/specs/openid-connect-eap-acr-values-1_0-final.html

https://datatracker.ietf.org/doc/html/rfc8176

https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims

https://damienbod.com/2025/12/20/digital-authentication-and-identity-validation/

https://damienbod.com/2025/07/02/implement-asp-net-core-openid-connect-with-keykloak-to-implement-level-of-authentication-loa-requirements/
