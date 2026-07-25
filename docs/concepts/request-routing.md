# Request routing

TruthGate decides whether a request belongs to the management application, a published site, a protected Kubo route, or a controller endpoint.

## Management requests

Requests by IP address are not treated as mapped site domains. They continue to the authenticated TruthGate application.

An unmapped hostname follows the same management path.

Typical examples:

```text
https://203.0.113.10/
https://admin.example.net/
```

## Mapped site requests

When the Host header matches a configured site domain, TruthGate routes the request to that site's published IPFS content.

Typical example:

```text
https://www.example.com/
```

The site path does not render the management portal.

## Protected Kubo routes

TruthGate can expose supported forms of:

```text
/ipfs/
/ipns/
/api/v0/
/webui/
```

These pass through TruthGate's authentication, API-key, TLS, and request-protection behavior rather than publishing Kubo's local listeners directly.

## Public metadata

Small read-only domain metadata endpoints are intentionally anonymous. They reveal deployment pointers such as CIDs and IPNS/TGP state, not private keys.

## Static framework assets and Blazor circuits

The management host must serve:

```text
/_framework/
/_content/
/_blazor
```

These are TruthGate application assets and circuit endpoints. They are unrelated to the assets of a separately published site.

## Order matters

The pipeline applies error handling, request protection, authentication, controllers, mapped-domain behavior, management-host guarding, static assets, and Razor components in deliberate order.

Changing middleware order can alter security or make a site route capture management traffic. Route changes should include tests for:

- raw IP portal;
- unmapped management host;
- mapped site host;
- authenticated Kubo routes;
- anonymous metadata;
- Blazor negotiate and WebSocket behavior.
