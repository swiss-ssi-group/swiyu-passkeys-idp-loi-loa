# Vectors of Trust

Using default values from RFC 8485

## A.1.  Identity Proofing

P0:  No proofing is done, and data is not guaranteed to be persistent
    across sessions

P1:  Attributes are self-asserted but consistent over time,
    potentially pseudonymous

P2:  Identity has been proofed either in person or remotely using
    trusted mechanisms (such as social proofing)

P3:  There is a binding relationship between the identity provider
    and the identified party (such as signed/notarized documents and
    employment records)

## A.2.  Primary Credential Usage

C0:  No credential is used / anonymous public service

Ca:  Simple session HTTP cookies (with nothing else)

Cb:  Known device, such as those indicated through device posture or
    device management systems

Cc:  Shared secret, such as a username and password combination

Cd:  Cryptographic proof of key possession using shared key

Ce:  Cryptographic proof of key possession using asymmetric key

Cf:  Sealed hardware token / keys stored in a trusted platform module

Cg:  Locally verified biometric

##  A.3.  Primary Credential Management


Ma:  Self-asserted primary credentials (user chooses their own
    credentials and must rotate or revoke them manually) / no
    additional verification for primary credential issuance or
    rotation

Mb:  Remote issuance and rotation / use of backup recover credentials
    (such as email verification) / deletion on user request

Mc:  Full proofing required for each issuance and rotation /
    revocation on suspicious activity

## A.4.  Assertion Presentation

Aa:  No protection / unsigned bearer identifier (such as an HTTP
    session cookie in a web browser)

Ab:  Signed and verifiable assertion, passed through the user agent
    (web browser)

Ac:  Signed and verifiable assertion, passed through a back channel

Ad:  Assertion encrypted to the RP's key

## Examples

P: OpenIDVP | C: Passkeys
"vot": "P3.Cf.Ma.Ac"

P: Email | C: Passkeys
"vot": "P1.Cf.Ma.Ac"

P: Email | C: Password
"vot": "P1.Cc.Ma.Ac"

P: OpenIDVP | C: Password
"vot": "P3.Cc.Ma.Ac"

P: OpenIDVP | C: OpenIDVP
"vot": "P3.Ce.Ma.Ac"

P: OpenIDVP | C: Password, OpenIDVP
"vot": "P3.Ce.Cc.Ma.Ac"

P: OpenIDVP | C: Password, passkeys
"vot": "P3.Cf.Cc.Ma.Ac"


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

## Links

https://www.rfc-editor.org/rfc/rfc8485.html

NIST standards:

https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-63-2.pdf

https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-63-3.pdf