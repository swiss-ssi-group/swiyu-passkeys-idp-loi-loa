# Implement swiyu identification & passkeys authentication using Duende IDP 

[![.NET](https://github.com/swiss-ssi-group/swiyu-passkeys-idp-loi-loa/actions/workflows/dotnet.yml/badge.svg)](https://github.com/swiss-ssi-group/swiyu-passkeys-idp-loi-loa/actions/workflows/dotnet.yml)

Example fo Aspire implementing Level of Identification (LoI) and Level of Authentication (LoA) using swiyu passkeys authentication in Duende IdentityServer with ASP.NET Core Identity.

## Blogs
- [Digital authentication and identity validation](https://damienbod.com/2025/12/20/digital-authentication-and-identity-validation/)
- [Set the amr claim when using passkeys authentication in ASP.NET Core](https://damienbod.com/2026/01/05/set-the-amr-claim-when-using-passkeys-authentication-in-asp-net-core/)
- [Implementing Level of Authentication (LoA) with ASP.NET Core Identity and Duende](https://damienbod.com/2026/01/12/implementing-level-of-authentication-loa-with-asp-net-core-identity-and-duende/)
- [Implementing Level of Identification (LoI) with ASP.NET Core Identity and Duende](https://damienbod.com/2026/01/18/implementing-level-of-identification-loi-with-asp-net-core-identity-and-duende/)
- [Force step up authentication in web applications](https://damienbod.com/2026/01/26/force-step-up-authentication-in-web-applications/)
- [Use client assertions in ASP.NET Core using OpenID Connect, OAuth DPoP and OAuth PAR](https://damienbod.com/2026/02/02/use-client-assertions-in-asp-net-core-using-openid-connect-oauth-dpop-and-oauth-par/)
- [Secure the swiyu container using a YARP proxy](https://damienbod.com/2026/02/09/isolate-the-swiyu-public-beta-management-apis-using-yarp/)
- [Planned: Secure the swiyu container using OAuth](https://damienbod.com)

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

## Getting started:

- [swiyu](https://swiyu-admin-ch.github.io/cookbooks/onboarding-base-and-trust-registry/)
- [Duende](https://duendesoftware.com/)

## Podman

https://podman-desktop.io/docs/troubleshooting/troubleshooting-podman

```bash
podman machine start
```

## ef DB Migrations
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Used OSS packages, containers, repositories 

- ImageMagick: https://github.com/manuelbl/QrCodeGenerator/tree/master/Demo-ImageMagick
- Microsoft Aspire: https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview
- Net.Codecrete.QrCodeGenerator: https://github.com/manuelbl/QrCodeGenerator/
- swiyu
  - https://github.com/swiyu-admin-ch/swiyu-verifier

## Links

https://github.com/swiyu-admin-ch/swiyu-verifier/issues/223

https://github.com/dotnet/aspnetcore/issues/64881

https://openid.net/specs/openid-connect-eap-acr-values-1_0-final.html

https://datatracker.ietf.org/doc/html/rfc8176

https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims

https://damienbod.com/2025/12/20/digital-authentication-and-identity-validation/

https://damienbod.com/2025/07/02/implement-asp-net-core-openid-connect-with-keykloak-to-implement-level-of-authentication-loa-requirements/

https://github.com/DuendeSoftware/samples/tree/main/IdentityServer/v7/UserInteraction/StepUp

https://datatracker.ietf.org/doc/rfc9470/

https://duendesoftware.com/blog/20250708-step-up-challenges-with-duende-identityserver-and-aspnet-core-apis

https://www.rfc-editor.org/rfc/rfc8485.html

https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-63-2.pdf

https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-63-3.pdf

## SSI

https://www.eid.admin.ch/en/public-beta-e

https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview

https://www.npmjs.com/package/ngrok

https://swiyu-admin-ch.github.io/specifications/interoperability-profile/

https://andrewlock.net/converting-a-docker-compose-file-to-aspire/

https://swiyu-admin-ch.github.io/cookbooks/onboarding-generic-verifier/

https://github.com/orgs/swiyu-admin-ch/projects/2/views/2

### SSI Standards

https://identity.foundation/trustdidweb/

https://openid.net/specs/openid-4-verifiable-credential-issuance-1_0.html

https://openid.net/specs/openid-4-verifiable-presentations-1_0.html

https://datatracker.ietf.org/doc/draft-ietf-oauth-selective-disclosure-jwt/

https://datatracker.ietf.org/doc/draft-ietf-oauth-sd-jwt-vc/

https://datatracker.ietf.org/doc/draft-ietf-oauth-status-list/

https://www.w3.org/TR/vc-data-model-2.0/

