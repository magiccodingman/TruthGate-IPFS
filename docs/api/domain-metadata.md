# Public domain metadata API

Published domains expose read-only deployment metadata.

Base path:

```text
https://YOUR_DOMAIN/api/truthgate/v1
```

Call these endpoints on the published domain. The Host header identifies the domain configuration.

## Current domain CID

```http
GET /api/truthgate/v1/GetDomainCid
```

Example:

```json
{
  "domain": "example.com",
  "cidV0": "QmExample...",
  "cidV1": "bafyExample..."
}
```

Use this endpoint for:

- deployment verification;
- gateway URLs;
- monitoring;
- CID-aware clients.

## Current IPNS and TGP state

```http
GET /api/truthgate/v1/GetDomainIpns
```

Example:

```json
{
  "domain": "example.com",
  "ipnsPeerId": "k51...",
  "tgpCid": "bafy...",
  "currentCid": "bafy...",
  "lastPublishedCid": "bafy..."
}
```

Field meaning:

- `ipnsPeerId` — the configured IPNS identity;
- `tgpCid` — the CID of the current TGP root;
- `currentCid` — the active target from `tgp.json`;
- `lastPublishedCid` — the latest site root recorded by TruthGate.

## Examples

```bash
curl -s \
  https://example.com/api/truthgate/v1/GetDomainCid \
  | jq
```

```bash
curl -s \
  https://example.com/api/truthgate/v1/GetDomainIpns \
  | jq
```

## Security model

These endpoints are anonymous and intentionally reveal public deployment pointers. They do not return private keys, account data, or API credentials.

## Caching

Metadata may be cached briefly. Monitoring should tolerate a short delay after publication and compare both the immutable site CID and mutable IPNS/TGP state.
