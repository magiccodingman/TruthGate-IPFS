# Kubo API proxy

TruthGate can proxy supported Kubo RPC operations under:

```text
/api/v0/
```

The local Kubo RPC API remains:

```text
/ip4/127.0.0.1/tcp/5001
```

inside the appliance.

## Why proxy it

Kubo's RPC API is an administrative interface. Publishing it directly would bypass TruthGate's:

- TLS termination;
- account and API-key policy;
- rate protection;
- audit and error behavior;
- route restrictions.

## Example

```bash
curl \
  -H 'X-API-Key: YOUR_KEY' \
  'https://YOUR_HOST/api/v0/id'
```

Exact methods and parameters follow the Kubo API for operations TruthGate permits.

## Compatibility

TruthGate bundles a specific Kubo version. Check `.env.example` or the image labels before relying on a Kubo method introduced in a newer release.

## Scope

Do not assume every Kubo RPC route is safe or supported through the public proxy. External documentation should name supported operations explicitly as that contract is stabilized.

## Native WebUI

The native WebUI uses Kubo APIs through the authenticated TruthGate path. It should be opened from the portal rather than by publishing Kubo's local gateway.
