# API authentication

## API keys

Create keys from the authenticated API settings page.

![Creating an API key](../../content/images/demo/api-add-dark.webp)

A key is displayed only when created. Store it in a password manager or secret store.

TruthGate stores a hash rather than a recoverable copy. Losing the original value requires creating a new key.

## Header

Preferred:

```http
X-API-Key: YOUR_KEY
```

## Query string

Legacy or convenience forms may be accepted on supported routes:

```text
?api_key=YOUR_KEY
?key=YOUR_KEY
```

Headers are preferred because URLs are more likely to appear in browser history, proxy logs, analytics, screenshots, and copied links.

## Session authentication

Browser-based management uses the login session. Do not build automation that scrapes login forms when API keys are available.

## Responses

Invalid, missing, and suspended credentials should not reveal which condition occurred. A banned client may be rejected before credential processing.

## Rotation

1. Create a replacement key.
2. Update the client.
3. Verify successful requests.
4. Revoke the old key.

Never commit API keys to source control or `.env.example`.
