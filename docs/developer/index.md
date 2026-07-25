# Developer guide

Development uses the same appliance contract as production.

## Start

```bash
docker compose \
  -f compose.yaml \
  -f compose.dev.yaml \
  up --build
```

Open:

```text
http://localhost:8080
```

Retrieve the first-run password:

```bash
docker compose \
  -f compose.yaml \
  -f compose.dev.yaml \
  logs truthgate
```

## Guides

- [Compose model](docker-compose.md)
- [Rider setup](rider.md)
- [Visual Studio](visual-studio.md)
- [Testing](testing.md)
- [Documentation contributions](documentation.md)

## Important local rule

Do not run another Kubo/IPFS Desktop node on the same machine with the default ports while debugging TruthGate. Both will normally want swarm TCP and UDP port `4001`, and conflicting local gateways or RPC assumptions can make failures confusing.

Stop the other node or deliberately change the development swarm host port.

## Source layout principle

Production behavior belongs in the production definitions and source. Development overrides should only change what is required to develop.

A feature that needs a new environment variable, port, volume, or runtime dependency should normally be added to `compose.yaml` first and then adjusted in `compose.dev.yaml` only when development differs.
