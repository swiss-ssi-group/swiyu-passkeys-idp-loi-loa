# STRIDE threat model and security assessment

**Review date:** 2026-11-05

**Reviewed upstream:** `swiss-ssi-group/swiyu-passkeys-idp-loi-loa`

**Baseline:** `main`, commit `3a7fdbb4e961abce0bd9eaae5c3aa1e23d3226f0` (tag `2.1.0`)

## Executive assessment

**Good protocol foundations, but not ready to serve as a production,
high-assurance identity provider without remediation.** Code flow, PAR,
DPoP, framework WebAuthn validation, and explicit API claim checks are
meaningful strengths. They do not compensate for weaknesses in the custom
wallet-to-account and account-lifecycle logic.

The highest-priority problems are account selection using self-declared
Beta-ID attributes and wallet verification results that are not bound to
the initiating browser or consumed once. An anonymous confirmation page
also discloses valid email-confirmation links. These are source-confirmed
mechanisms, not demonstrated compromises of a running deployment.

The repository is an example using sandbox credentials. Severity reflects
what would happen if the implementation protected real accounts or
resources; it does not assert that a production system does so. In
particular, `loi.400`, `loa.400`, and VoT claims are application assertions,
not independent evidence of regulatory assurance or certification.

## Scope, method, and limits

Static inspection covered HTTP security controls, OAuth/OIDC configuration,
passkeys, sessions, wallet verification, assurance claims, API authorization,
management integration, account lifecycle, and declared dependency versions.
Exact upstream framework implementations and public advisory metadata were
consulted where needed. No application, repository script, or live-service
attack was executed. This document changes no runtime behavior.

Findings are separated into source-confirmed defects, deployment-dependent
risks, and unverified assumptions. No penetration test, exhaustive audit,
formal proof, or OIDC conformance certification is implied. Severity and
confidence are qualitative reviewer judgments, not measured CVSS scores.

## System, assets, and trust boundaries

```text
Browser / authenticator                  Wallet holding Beta-ID
        | HTTPS, cookies, WebAuthn                 |
        v                                         | OID4VP
Identity provider (Identity + Duende)     Public YARP presentation proxy
        |                   |                      |
        |                   +-- management OAuth --> swiyu verifier
        |                                            (private API)
        +-- SQL Server: users, passkeys, identity associations
        |
        +-- OIDC code flow / PAR / client authentication --> Blazor web client
                                                               |
                                                          JWT + DPoP
                                                               v
                                                     Protected API

Aspire orchestrates services, secrets/configuration, discovery and telemetry.
A separate swiyu issuer container is described as testing-only.
TLS termination and management-network exposure are deployment assumptions.
```

| Asset | Required security property |
|---|---|
| Local account and wallet association | Only an authorized holder can establish or change the association. |
| Passkey public keys and enrollment | New authenticators cannot be enrolled through an insufficiently trusted session. |
| Identity cookies, codes, tokens, DPoP keys | Confidentiality, transaction binding, expiration, and appropriate replay prevention. |
| LOI/LOA/AMR/VoT claims | Derive from justified, current evidence; never elevate sandbox evidence into production assurance. |
| Issuer signing keys and management credentials | Restricted access, rotation, audience separation, and no diagnostic disclosure. |
| Personal attributes, recovery links, telemetry | Least disclosure, integrity, appropriate retention, and restricted operator access. |
| Authentication availability | Bounded work and dependency failures that do not bypass authentication. |

Important trust boundaries are:

1. Unauthenticated browser input into identity-provider account endpoints.
2. Browser-provided verification identifiers into privileged verifier-management calls.
3. Verified credential attributes into local account selection and assurance claims.
4. An existing session into enrollment of a future high-assurance authenticator.
5. Identity provider to OIDC client, and OIDC client to resource API.
6. Public YARP routes into internal services and their management interfaces.
7. Deployment configuration, secrets, database, and telemetry into operator control.

Threat actors include unauthenticated remote users, holders of their own
genuine sandbox credentials, attackers possessing disclosed presentation
identifiers, attackers with an existing weak or stolen session, and
operators or compromised internal services. A valid signed credential is
not automatically proof of entitlement to an existing local account.

## Existing security strengths

- The registered OIDC client uses authorization-code flow, certificate-based
  authentication, mandatory PAR and DPoP, and configured exact callback URLs.
  Duende 8.0.10 defaults require PKCE and reject plain PKCE; these defaults
  are not disabled (`Idp.Swiyu.Passkeys.Sts/Config.cs:37-67`).
- The protected API validates issuer, audience, and `at+jwt` token type,
  validates DPoP, and requires both `loa.400` and `loi.400`.
- WebAuthn cryptography is delegated to ASP.NET Core Identity. Origin
  validation is pinned to the configured authority; framework defaults
  require user verification
  (`Idp.Swiyu.Passkeys.Sts/HostingExtensions.cs:92-105`). The default was
  verified against framework v10.0.0 source, not a deployed runtime inventory.
- Passkey option endpoints explicitly check antiforgery tokens. Razor
  Pages retain framework antiforgery protection. This does not bind a
  submitted wallet verification ID to its owner.
- YARP exposes `/oid4vp/**`, `/oid4vci/**`, and issuer discovery, not
  `/management/**`. Production seeding is empty.

## Source-confirmed findings

Paths below are relative to the reviewed root. Line references describe
the baseline commit, not future versions.

| ID | Severity | Confidence | Finding | Source |
|---|---|---|---|---|
| F1 | HIGH | 10/10 | Self-declared Beta-ID attributes can select an existing account. | `Idp.Swiyu.Passkeys.Sts/SwiyuServices/VerificationService.cs:34-43,200-230`; `Pages/Swiyu/Login.cshtml.cs:123-149,193-207` under the STS project |
| F2 | HIGH | 9/10 | Wallet verification results are reusable bearer login credentials. | `Idp.Swiyu.Passkeys.Sts/Pages/Swiyu/Login.cshtml.cs:38-39,87-99,104-149,173-185`; `Pages/Swiyu/Register.cshtml.cs:26-27,64-111` under the STS project |
| F3 | MEDIUM | 10/10 | An anonymous page exposes valid email-confirmation tokens. | `Idp.Swiyu.Passkeys.Sts/Pages/Account/RegisterConfirmation.cshtml.cs:12,30-55`; sibling `RegisterConfirmation.cshtml:9-14` |
| F4 | MEDIUM | 9/10 | Deleted users remain active for otherwise-valid outstanding OAuth grants. | `Idp.Swiyu.Passkeys.Sts/ProfileService.cs:46-88`; `HostingExtensions.cs:129-131`; `Pages/Account/Manage/DeletePersonalData.cshtml.cs:67-74` under the STS project |
| F5 | MEDIUM | 10/10 | Post-authentication return URLs are not constrained to trusted destinations. | `Idp.Swiyu.Passkeys.Web/MapLoginLogoutEndpoints.cs:12-40`; `Idp.Swiyu.Passkeys.Sts/Pages/Swiyu/Login.cshtml.cs:118-119,151-161` |

### F1: Beta-ID account impersonation

Wallet login matches `birth_date`, `birth_place`, `given_name`, and
`family_name` to a local account, signs it in, and asserts `loi.400`.
The verifier requests Beta-ID credentials, whose official documentation
explicitly permits chosen attribute values and disclaims identification.
An attacker who knows the four attributes can obtain their own genuine
Beta-ID carrying them and authenticate as the matched account. Forging
issuer signatures or possessing the victim's wallet key is not required.
Holder binding proves control of that credential, not ownership of the
demographically matched account. The resulting session can reach passkey
enrollment.

**Remediation:** Keep Beta-ID in isolated test accounts, issuers, clients,
and audiences. For production, require appropriately proofed credentials
and an explicit, securely established account association rather than
demographic equality. Define how reassociation and duplicate attributes
are handled without silently merging identities.

### F2: Verification replay and cross-browser/cross-purpose substitution

Login and registration accept a posted `VerificationId` and retrieve it
using server management credentials. Acceptance depends on verifier state
`SUCCESS`, not a server-side binding to the browser, operation, subject,
or OIDC transaction. Successful results are not consumed; the modeled
`request_nonce` is not compared with browser-associated state.

Upstream verifier 5.0.1 places the management identifier in the wallet
request-object URL and returns successful results repeatedly until
expiration. An attacker who obtains a victim's presentation identifier
can submit it with the attacker's own valid form/antiforgery token after
the victim completes verification. This can create a session as the
matched user without fresh wallet approval. Identifiers can also cross
the login/enrollment purpose boundary. This is bounded by verifier
expiration/retention, not necessarily indefinite replay.

**Remediation:** Persist pending transactions server-side, bound to
browser state, purpose, intended account where applicable, nonce, and
authorization context. Require the exact expected transaction and
atomically consume it once. Reject expired, substituted, already-used,
and wrong-purpose results before account lookup or mutation.

### F3: Mailbox-ownership verification bypass

The anonymous confirmation page accepts an existing email address,
generates a valid confirmation token, and displays its link.
`DisplayConfirmAccountLink = true` has no development-only guard. The
normal `ConfirmEmailAsync` endpoint accepts the generated token.
An attacker can register using someone else's address and confirm it
without mailbox access, or confirm an existing unconfirmed address.
This alone does not reset an existing password or disclose a reset token.

**Remediation:** Remove token generation/rendering from this page and
deliver confirmation links only through the intended delivery channel.
Use non-enumerating responses for unknown or already-confirmed accounts.

### F4: Account deletion does not invalidate outstanding grants

The custom profile service always sets `IsActive = true`. Deletion removes
the Identity user and signs out the current session but does not revoke
outstanding grants. Duende 8.0.10 checks the profile service during code
and refresh-token redemption, so an otherwise-valid grant is not rejected
because the user has been deleted. Assurance claims are copied from the
grant subject.

Client authentication, PKCE, DPoP, and expiration checks still apply.
The included web client does not explicitly request `offline_access`;
longer-lived refresh-token exploitation requires such a grant actually
to have been issued. Immediate revocation of already-issued JWTs is a
separate resource-server design decision.

**Remediation:** Check current database-backed account existence/activity
and explicitly revoke grants and relevant sessions during deletion or
disablement. Define acceptable residual JWT lifetime and refresh behavior.

### F5: Post-login open redirects

Web `/login` and `/stepuploa` copy query `returnUrl` into
`AuthenticationProperties.RedirectUri` without a local-URL check.
Wallet login also redirects to posted `ReturnUrl` without requiring a
valid authorization context. A crafted link can send a user to an
attacker origin after authentication, enabling trusted-origin phishing.
This is not a bypass of the registered OIDC callback allowlist; code or
token exfiltration was not demonstrated.

**Remediation:** Accept only local application paths, or an explicit
narrow allowlist. Wallet login must use a validated authorization context
or a local destination. Reuse the ordinary login handler's validation.

## HTTP headers, cookies, ingress, and transport

These are source observations and acceptance criteria, not captured
production response headers.

| Surface | Observation and required control |
|---|---|
| STS transport | `HostingExtensions.cs:140-160` does not itself enforce HTTPS redirection/HSTS. Prove edge TLS/HSTS and block direct insecure backend access. |
| STS HTML headers | `Pages/SecurityHeadersAttribute.cs:15-54` intends nosniff, DENY framing, restrictive CSP, and no-referrer, but is not consistently decorated across account/wallet pages. It is an `ActionFilterAttribute`; action filters do not run on Razor Page handlers. Decoration is not proof of effective coverage. Use appropriate middleware or a Razor Pages filter and verify all relevant responses. |
| Web client | `SecurityHeadersDefinitions.cs:11-42` defines global nosniff, DENY framing, strict-origin-when-cross-origin, COOP/CORP same-origin, COEP require-corp, Permissions-Policy, CSP, and production HSTS. `Program.cs:105-131` registers/enables headers, HSTS, HTTPS redirection and antiforgery. These controls do not cover the separate STS origin. |
| Current web CSP | Includes `object-src 'none'`, `base-uri 'self'`, `frame-ancestors 'none'`, restricted form/image/font sources, nonced script policy with `'unsafe-inline'`, and inline styles. No explicit `default-src` is configured. Assess effective browser behavior and missing directive fallbacks rather than calling this a complete XSS defense. |
| CSP and framing | Verify enforcing CSP with restrictive `frame-ancestors`, `base-uri`, `object-src`, and tested script/style sources. Use nonces/hashes where needed; do not break Blazor, WebAuthn, or required wallet interactions. |
| Content and referrers | Verify `X-Content-Type-Options: nosniff`, correct MIME types, and an explicit restrictive Referrer-Policy on authentication/presentation pages. |
| Sensitive caching | Require `Cache-Control: no-store` on token-bearing and sensitive account/presentation responses, not indiscriminately on public static assets. |
| Cookies | Web `Program.cs:46-52` sets `__Host-idp-swiyu-passkeys-web` and SameSite=Lax, leaving other cookie settings to framework defaults; line 65 enables `SaveTokens`. Inspect actual cookies behind TLS termination for Secure, HttpOnly where appropriate, host/path scoping, lifetime, and ticket size/storage. Preserve OIDC correlation/nonce flows; do not blindly set every cookie to Strict. |
| Host/proxy trust | Pin public origins and allowed hosts; accept forwarded scheme/host only from explicitly trusted proxies. No restrictive STS host/proxy setup was established. |
| Internal management | AppHost config uses HTTP for verifier management. Require private reachability and assess TLS/mTLS across the actual network boundary. |
| Diagnostics | Complete-security-artifact logging is enabled outside a development guard. Restrict it and verify redaction; an actual production token disclosure was not demonstrated. |

HSTS subdomain coverage/preload needs an operational rollout plan.
`X-XSS-Protection` and `Expect-CT` are not substitutes for modern CSP/TLS.
Do not treat absent CORS headers as a vulnerability; same-origin
restrictions are protective by default.

## OAuth, OIDC, WebAuthn, and assurance assessment

**Preserve:** Code flow, PKCE defaults, PAR, asymmetric client
authentication, DPoP, callback allowlists, issuer/audience/type validation,
and framework WebAuthn checks.

**Repair:** Application redirects (F5), current-subject activity checks
(F4), wallet transaction binding (F2), and proof-to-account/LOI mapping
(F1). Exact callback registration does not make arbitrary application
return URLs safe. Antiforgery does not establish wallet-result ownership.
DPoP protects token use, not the identity proof used to mint the token.

**Validate in an authorized test environment:** OIDC state/correlation and
nonce handling, issuer/signature/audience/expiry rejection, PKCE downgrade
and code-replay rejection, logout/session termination, DPoP replay stores
across replicas, and authenticator enrollment/recovery assurance. These
are verification tasks, not additional confirmed defects.

## Deployment-dependent risks

| ID | Potential impact and prerequisite | Evidence and mitigation |
|---|---|---|
| R1 | HIGH: another issuer-valid token reaches management APIs. Requires network reachability and a token accepted by the configured issuer. | `Idp.Swiyu.Passkeys.AppHost/AppHost.cs:110,147` specifies management issuer without an audience restriction. Upstream verifier uses authenticated-only management access. Require intended audience and application permissions plus network isolation. Current YARP routes do not directly expose management. |
| R2 | HIGH: poisoned recovery URLs leak reset tokens. Requires accepted hostile Host/forwarded host, working email delivery, and victim interaction. | `Idp.Swiyu.Passkeys.Sts/Pages/Account/ForgotPassword.cshtml.cs:49-60` and `ProfileService.cs:120-126` use request-derived origins for recovery/`vtm`. Use configured canonical origins and ingress host/proxy restrictions. |
| R3 | HIGH: weak or stolen sessions enroll a passkey later receiving LOA400. Policy impact depends on the intended bootstrap model. | `Idp.Swiyu.Passkeys.Sts/Pages/Account/Passkeys.cshtml.cs:105-141`; `Pages/Account/Login/Index.cshtml.cs:113-124`. Require recent strong reauthentication for authenticator changes and a separately protected bootstrap/recovery flow. |
| R4 | Transport/header/diagnostic gaps increase disclosure and browser attack exposure if not covered at deployment. | `Idp.Swiyu.Passkeys.Sts/HostingExtensions.cs:140-160`; `Pages/SecurityHeadersAttribute.cs:15-54`; `Idp.Swiyu.Passkeys.AppHost/AppHost.cs:111-113,177-181,193-203`. Verify effective controls rather than assuming edge compensation. |

The configured `VERIFICATION_EXPIRY_MUST_BE_PRESENT=false` also requires a
documented justification before non-sandbox use. Credential validity and
presentation-transaction expiry are distinct checks.

## STRIDE threat register

S = spoofing, T = tampering, R = repudiation, I = information disclosure,
D = denial of service, E = elevation of privilege.

| ID | STRIDE | Boundary / scenario | Existing controls, residual risk, and treatment |
|---|---|---|---|
| T1 | S, E | Credential attributes select another user's account. | Signature/holder checks do not prove demographic account ownership. F1: isolate sandbox credentials and establish secure account bindings. |
| T2 | S, T, R | Presentation ID is substituted, replayed, or moved between login and enrollment. | Antiforgery and verifier success do not bind the browser or purpose. F2: server-bound, expiring, single-use transactions and audit correlation. |
| T3 | I, S | Anonymous visitor obtains another address's confirmation token. | Framework token signing is bypassed by intentional disclosure. F3: deliver tokens only through the mailbox channel. |
| T4 | E | Deleted subject redeems outstanding grants. | Protocol grant checks remain, but current account activity is not checked. F4: restore activity checks and revoke grants/sessions. |
| T5 | T, S | Authentication ends at an attacker-controlled destination. | OIDC callback allowlist is separate from application return URLs. F5: validate local destinations. |
| T6 | E | Weak session provisions a stronger authenticator. | WebAuthn attestation/assertion validity does not authorize enrollment. R3: enforce recent strong authorization and explicit bootstrap policy. |
| T7 | S, E | Wrong-audience token authorizes verifier management. | YARP route filtering helps; internal reachability remains an assumption. R1: audience, permission, and network restrictions. |
| T8 | T, I | Host/proxy manipulation changes recovery links or signed metadata origin. | Ingress filtering was not verified. R2: canonical origins, allowed hosts, and trusted proxy ranges. |
| T9 | I | Tokens or personal attributes enter diagnostics, caches, referrers, or insecure internal links. | Deployment controls unknown. R4: redact, minimize, restrict retention/access, and verify headers/transport. |
| T10 | R | Account linking, recovery, authenticator changes, or revocation cannot be attributed. | Audit completeness/retention not verified. Record transaction, subject, purpose, decision, and consumption events without tokens or raw presentations. |
| T11 | D | Expensive passkey options, wallet polling, management calls, or database work exhaust capacity. | Capacity/rate-limit testing was not performed. Bound pending transactions, polling, request sizes, concurrency and timeouts; rate-limit by suitable account/client/network signals. This is a modeled threat, not a confirmed exploitable DoS finding. |
| T12 | T, E | Compromised build, dependency, image, signing key, or operator changes identity decisions. | Full supply-chain/secret inventory not verified. Pin immutable images, audit access, rotate keys, scan restored dependencies/images, and protect CI permissions. |

## Known issues and dependency advisories

Public GitHub advisory queries for all 38 unique declared NuGet
package/version pairs succeeded, parsed valid JSON, and returned no
matching advisories (`[]`), including Duende IdentityServer and
AspNetIdentity `8.0.10`, token-management `4.3.0`, bearer integration
`1.0.2`, YARP `2.3.0`, Magick.NET `15.0.0`, and declared Microsoft packages
`10.0.14`. Public issuer/verifier repository advisory queries also returned
no entries. The declared swiyu container versions are `5.0.1`.

**No applicable CVE was confirmed. This is not a vulnerability-free verdict.**
No dependency restore, resolved transitive inventory, deployed SBOM,
runtime patch inventory, container digest, or OS-package scan was
available. Native/Maven/transitive dependencies and actual deployed image
contents remain unverified. Advisory coverage and publication dates can
change; age or a superficially similar product name is not CVE evidence.

Before production, restore the actual solution in an isolated environment,
run `dotnet list package --vulnerable --include-transitive`, inventory the
deployed .NET runtime, and scan images by digest including OS and Java/native
dependencies. Preserve tool/feed versions, scan time, and applicability
analysis. A package manifest alone is not the deployed inventory.

## Remediation and acceptance plan

| Priority | Action | Required acceptance evidence |
|---|---|---|
| P0 | Fix F1 and F2 before protecting real accounts. | A genuine attacker-owned Beta-ID with victim-like attributes cannot select the victim account; a valid result from another browser/purpose/account is rejected; two concurrent completions produce at most one success; expired/replayed results fail. |
| P1 | Remove confirmation-token disclosure (F3). | Anonymous requests for known/unknown emails return no token or confirmation URL; only delivery-channel possession completes confirmation. |
| P1 | Restore account lifecycle checks (F4). | Delete/disable a user with an outstanding code and, when supported, refresh token: otherwise-valid redemptions fail; document residual access-token lifetime. |
| P1 | Validate destinations (F5). | Login, step-up, and wallet completion reject absolute external URLs, scheme-relative URLs, and equivalent encoded/bypass forms; valid local/OIDC continuations work. |
| P1 | Establish enrollment and management assurance (R1, R3). | Weak sessions cannot silently add a privileged authenticator; wrong-audience/permission tokens cannot access management; management remains unreachable publicly. |
| P1 | Verify ingress, headers, cookies, diagnostics (R2, R4). | Check actual success/error responses and issued cookies through ingress; hostile hosts fail; recovery links use canonical origin; sensitive logs are redacted; insecure backend access is blocked. |
| P2 | Add protocol, operational, and supply-chain regressions. | Negative OIDC/DPoP tests, multi-replica replay checks, audit event coverage, bounded-resource testing, and resolved dependency/image scans run in CI or authorized staging. |

Owners should be assigned by role: identity/application maintainers for
F1-F5 and enrollment policy; platform operators for ingress, management
isolation, secrets and telemetry; security/release owners for assurance
approval and dependency evidence. No owner acceptance or risk sign-off is
implied by this document.

## Remaining unknowns

Actual ingress/TLS/host filtering, backend exposure, production account
usage, management audiences/roles, cookie flags after proxying, distributed
DPoP replay handling, secret provisioning, telemetry retention/access,
effective rate limits, and compensating deployment controls were not
verified. Reassess after remediation or any change to identity proofing,
account binding, credential type, authenticator recovery, or topology.

## Evidence and standards

Source paths above refer to the immutable
[reviewed snapshot](https://github.com/swiss-ssi-group/swiyu-passkeys-idp-loi-loa/tree/3a7fdbb4e961abce0bd9eaae5c3aa1e23d3226f0).
Key external evidence and assessment criteria:

- [Official Beta-ID limitations](https://github.com/swiyu-admin-ch/swiyu-admin-ch.github.io/blob/8485a1b00d8f80ffc0f263e22f5b5cbdc2dd8a9f/_cookbooks/how-to-use-beta-id.md#L23-L42).
- [Verifier 5.0.1 management mapper](https://github.com/swiyu-admin-ch/swiyu-verifier/blob/5.0.1/verifier-service/src/main/java/ch/admin/bj/swiyu/verifier/service/management/ManagementMapper.java#L44-L55).
- [Duende 8.0.10 code activity validation](https://github.com/DuendeSoftware/products/blob/is-8.0.10/identity-server/src/IdentityServer/Validation/Default/TokenRequestValidator.cs#L572-L581).
- [Duende 8.0.10 refresh activity validation](https://github.com/DuendeSoftware/products/blob/is-8.0.10/identity-server/src/IdentityServer/Services/Default/DefaultRefreshTokenService.cs#L130-L145).
- [OAuth security BCP, RFC 9700](https://www.rfc-editor.org/rfc/rfc9700.html).
- [OpenID Connect Core](https://openid.net/specs/openid-connect-core-1_0.html), particularly section 3.1.3.7 on ID Token validation.
- [DPoP, RFC 9449](https://www.rfc-editor.org/rfc/rfc9449.html).
- [OWASP HTTP Headers Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Headers_Cheat_Sheet.html).
- [OWASP OAuth2 Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/OAuth2_Cheat_Sheet.html).
- [Microsoft STRIDE threat categories](https://learn.microsoft.com/en-us/azure/security/develop/threat-modeling-tool-threats).
