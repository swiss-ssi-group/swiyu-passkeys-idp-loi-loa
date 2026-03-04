## Create Swiyu DID space

See: https://swiyu-admin-ch.github.io/cookbooks/onboarding-base-and-trust-registry/#create-did-space

$SWIYU_IDENTIFIER_REGISTRY_ACCESS_TOKEN: Select from correct application
$SWIYU_PARTNER_ID: https://portal.trust-infra.swiyu-int.admin.ch/ui/organizations
$SWIYU_IDENTIFIER_REGISTRY_URL: https://swiyu-admin-ch.github.io/cookbooks/onboarding-base-and-trust-registry/#base-urls

> Note: Use a valid access token.

```
curl \
  -H "Authorization: Bearer $SWIYU_IDENTIFIER_REGISTRY_ACCESS_TOKEN" \
  -X POST "$SWIYU_IDENTIFIER_REGISTRY_URL/api/v1/identifier/business-entities/$SWIYU_PARTNER_ID/identifier-entries"
```

Docs: https://swiyu-admin-ch.github.io/cookbooks/onboarding-base-and-trust-registry/#create-a-did-or-create-the-did-log-you-need-to-continue
Download: https://github.com/swiyu-admin-ch/didtoolbox-java/releases

# Create Your First DID

https://swiyu-admin-ch.github.io/cookbooks/onboarding-base-and-trust-registry/#command-syntax

```
java -jar didtoolbox.jar create --identifier-registry-url $IDENTIFIER_REGISTRY_URL
```

## Example
java -jar didtoolbox.jar create --identifier-registry-url https://identifier-reg.trust-infra.swiyu-int.admin.ch/api/v1/did/cd692f1a-b322-44bb-8396-9e87cc3af692


# Upload DID log content

https://swiyu-admin-ch.github.io/cookbooks/onboarding-base-and-trust-registry/#upload-did-log

$SWIYU_IDENTIFIER_REGISTRY_ACCESS_TOKEN: Select from correct application
$YOUR_GENERATED_DIDLOG: console result from the **Create Your First DID** didtoolbox.jar request
$SWIYU_PARTNER_ID: https://portal.trust-infra.swiyu-int.admin.ch/ui/organizations
$IDENTIFIER_REGISTRY_ID: ID returned when the **Create Swiyu DID space** was created

```
curl \
  -H "Authorization: Bearer $SWIYU_IDENTIFIER_REGISTRY_ACCESS_TOKEN" \
  -H "Content-Type: application/jsonl+json" \
  -d '$YOUR_GENERATED_DIDLOG' \
  -X PUT "https://identifier-reg-api.trust-infra.swiyu-int.admin.ch/api/v1/identifier/business-entities/$SWIYU_PARTNER_ID/identifier-entries/$IDENTIFIER_REGISTRY_ID"
```
