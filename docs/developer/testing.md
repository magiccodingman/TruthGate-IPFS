# Testing

## Local development matrix

Test:

- management portal by IP/localhost;
- login and logout;
- authenticated native IPFS WebUI;
- CID pin management;
- watched IPNS updates;
- API-key creation and use;
- mapped domain routing;
- SPA fallback;
- public domain metadata;
- TGP validation;
- container recreation with persistent state.

## Browser contexts

Useful separate contexts include:

- normal browser profile;
- private/incognito profile;
- IPFS-aware browser profile when testing Companion behavior.

Do not rely on one cached authenticated browser session for every routing test.

## Container checks

```bash
docker compose ps
docker exec truthgate truthgate-kubo-status
```

## Build targets

Test both:

```bash
docker build --target production .
docker build --target development .
```

## Compose validation

```bash
docker compose -f compose.yaml config
docker compose -f compose.yaml -f compose.dev.yaml config
```

## Blazor regression check

```bash
curl -skf \
  -H 'Accept-Encoding: identity' \
  https://127.0.0.1/_framework/blazor.web.js \
  -o /tmp/blazor.web.js

test "$(wc -c </tmp/blazor.web.js)" -gt 10000
```

## Architecture CI

The repository's appliance and legacy-migration workflows run native AMD64 and ARM64 jobs. Do not replace those with one emulated smoke test without understanding what coverage is lost.

## External IPFS validation

Use another node:

```bash
ipfs routing findpeer <PEER_ID>
ipfs cat /ipfs/<NEW_CID>/index.html
```

This verifies more than local RPC health.
