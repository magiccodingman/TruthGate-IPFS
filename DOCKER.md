# TruthGate Docker appliance

TruthGate ships as one container containing the TruthGate ASP.NET application,
Kubo, and the matching `ipfs` CLI. Docker owns the software lifecycle; mounted
state survives image replacement.

The appliance is intentionally configured as a contributing IPFS server. It is
not a quiet desktop-node preset: DHT server mode, inbound TCP/UDP swarm
transports, content providing, and automatic repository GC are enabled unless
an operator explicitly overrides them.

## Requirements

- Docker Engine with the Compose v2 plugin
- Docker Compose 2.24.4 or newer for the development override's `!override`
  merge directive
- TCP ports 80, 443, and 4001 plus UDP port 4001 available by default
- Public TCP and UDP forwarding for the selected swarm port when deployed
  behind NAT

## Production quick start

```bash
git clone https://github.com/magiccodingman/TruthGate-IPFS.git
cd TruthGate-IPFS
cp .env.example .env
docker compose pull
docker compose up -d
```

The default Compose image is `magiccodingman/truthgate-ipfs:stable`. Open
`https://localhost` after the container becomes healthy. The first connection
uses TruthGate's self-signed fallback certificate unless a configured domain
has an issued certificate.

On the first boot, retrieve the generated administrator password with:

```bash
docker compose logs truthgate
```

The username is `admin`. Change its password in the TruthGate UI. Until the
configuration is persisted, the bootstrap password is also retained at
`data/truthgate/state/bootstrap-admin-password` with restrictive permissions.

To build the production image locally instead of pulling the stable release:

```bash
docker compose up --build -d
```

## Published image tags

A successful promotion to the protected `stable` branch runs the complete
AMD64/ARM64 appliance, legacy-repository migration, and TLS lifecycle gates
before Docker Hub publishing starts.

The release workflow publishes:

```text
magiccodingman/truthgate-ipfs:stable
magiccodingman/truthgate-ipfs:latest
magiccodingman/truthgate-ipfs:0.1
magiccodingman/truthgate-ipfs:0.1.0
magiccodingman/truthgate-ipfs:sha-<commit>
```

`stable` and `latest` move to the newest successful stable release. The
major/minor series tag, such as `0.1`, also moves forward. Full semantic
versions are immutable release identifiers. `VERSION` contains the intentional
major/minor series; every successful stable promotion automatically allocates
the next patch version.

The same multi-platform tag resolves to the correct `linux/amd64` or
`linux/arm64` image automatically. Replacing the container does not replace
mounted state.

To update an existing deployment:

```bash
docker compose pull
docker compose up -d
```

Pin `TRUTHGATE_IMAGE` in `.env` to a full version when an installation should
not automatically follow the moving `stable` tag.

## Persistent storage contract

The default deployment keeps three host paths separate:

```text
data/
├── truthgate/       # config, database, certificates, secrets, app state
└── ipfs/
    ├── repo/        # Kubo identity, config, datastore, keystore, repo version
    └── blocks/      # Kubo block files
```

They are mounted as:

```text
/data/truthgate
/data/ipfs/repo
/data/ipfs/repo/blocks
```

Kubo still sees a conventional `$IPFS_PATH/blocks` directory, while the host
can later relocate only the blockstore by changing `IPFS_BLOCKS_HOST_PATH`.
No network filesystem, FUSE, JuiceFS, mount management, or mount health logic
is included in this implementation.

The software inside the image is disposable. Backups should target the mounted
state, not the .NET runtime, Kubo binary, application binaries, or build output.

## Opinionated Kubo server defaults

A new repository is initialized with Kubo's `server` profile. An existing
repository receives that profile once, tracked by
`data/truthgate/state/kubo-server-profile-v1`. The profile disables mDNS and
automatic NAT port mapping and filters non-public address ranges, matching a
public server deployment. Set `TRUTHGATE_KUBO_APPLY_SERVER_PROFILE=false`
before the first managed boot when that behavior is not appropriate.

TruthGate then manages these defaults:

```text
Routing.Type                     dhtserver
Addresses.Swarm                  current Kubo defaults, with missing
                                 TCP/QUIC/WebTransport listeners restored
Swarm.EnableHolePunching         true
Swarm.RelayClient.Enabled        true
Provide.Enabled                  true
Provide.Strategy                 all
Provide.DHT.Interval             22h
Provide.DHT.SweepEnabled         true
Provide.DHT.ResumeEnabled        true
Datastore.StorageMax             auto: 90% of the blockstore filesystem
Datastore.StorageGCWatermark     90
Automatic repository GC         enabled
Addresses.API                    /ip4/127.0.0.1/tcp/5001
Addresses.Gateway                /ip4/127.0.0.1/tcp/9010
```

The entrypoint preserves Kubo's current listener array and only appends missing
standard listeners. That avoids deleting newer transports such as
WebTransport when Kubo changes its defaults.

The RPC API and HTTP gateway are always loopback-only. TruthGate already
authenticates and proxies public `/api/v0`, `/ipfs`, `/ipns`, and WebUI access.
Those two Kubo listener settings cannot be overridden.

The public swarm mapping on port 4001 is the current implementation stage.
TruthGate is expected to own and proxy swarm transports in a future
architecture; until that transport layer exists, Docker publishes Kubo's TCP
and UDP swarm traffic directly.

## Persistent Kubo settings

On first boot TruthGate creates:

```text
data/truthgate/config/kubo-settings.json
```

This file is persistent and contains the normal managed settings. It is also
the contract intended for future live management from the TruthGate UI.
Editing the file and restarting the container changes the effective Kubo
configuration without rebuilding the image.

Environment variables take precedence over the persistent settings file. The
available variables are documented in `.env.example`, including:

```text
TRUTHGATE_KUBO_ROUTING_TYPE
TRUTHGATE_KUBO_PROVIDE_ENABLED
TRUTHGATE_KUBO_PROVIDE_STRATEGY
TRUTHGATE_KUBO_STORAGE_MAX
TRUTHGATE_KUBO_STORAGE_PERCENT
TRUTHGATE_KUBO_ENABLE_GC
TRUTHGATE_KUBO_PUBLIC_IPV4
TRUTHGATE_KUBO_PUBLIC_IPV6
TRUTHGATE_KUBO_ANNOUNCE_PORT
```

For advanced settings, edit:

```text
data/truthgate/config/kubo-overrides.json
```

It is a JSON object whose keys are Kubo config paths and whose values are raw
JSON values:

```json
{
  "Swarm.ConnMgr.LowWater": 100,
  "Swarm.ConnMgr.HighWater": 300,
  "Routing.AcceleratedDHTClient": false
}
```

Advanced overrides run after TruthGate's normal defaults. `Addresses.API` and
`Addresses.Gateway` are ignored in this file because they are protected
loopback interfaces.

## Storage policy

The default storage policy is:

```json
{
  "storage": {
    "max": "auto",
    "percent": 90,
    "fallback": "200GB",
    "gcWatermark": 90,
    "enableGc": true
  }
}
```

`auto` reads the total capacity of the filesystem containing
`/data/ipfs/repo/blocks` and sets Kubo's `Datastore.StorageMax` to 90 percent of
that total. It uses total capacity rather than currently free space so the
limit does not shrink as content is added. If capacity detection fails, the
fallback is `200GB`.

A fixed value is also supported:

```env
TRUTHGATE_KUBO_STORAGE_MAX=2TB
```

Auto mode recalculates on every container start, so expanding a VPS disk only
requires a container restart. Fixed mode remains exactly the configured value.
`StorageMax` is a soft Kubo blockstore/GC threshold rather than a hard quota,
so filesystem and metadata headroom still matter.

## Public announce addresses

By default TruthGate attempts to detect public IPv4 and IPv6 addresses and
manages matching TCP, QUIC-v1, and WebTransport entries in
`Addresses.AppendAnnounce`. Previously managed entries are removed before new
ones are added, so public-IP changes do not accumulate stale addresses.

Each address can be automatic, disabled, or literal:

```env
TRUTHGATE_KUBO_PUBLIC_IPV4=auto
TRUTHGATE_KUBO_PUBLIC_IPV6=off
TRUTHGATE_KUBO_ANNOUNCE_PORT=4001
```

The announce port defaults to `IPFS_SWARM_PORT`. If Docker publishes
`14001:4001`, set `IPFS_SWARM_PORT=14001`; TruthGate will announce `14001` while
Kubo continues listening on container port 4001.

## Kubo startup and migrations

Every start uses automatic repository migration. Automatic GC is included when
enabled:

```bash
ipfs daemon --migrate=true --enable-gc
```

The entrypoint applies managed settings before starting the daemon, then starts
TruthGate only after Kubo's loopback RPC API is ready.

## Diagnostics

Inspect the effective configuration and live node state with:

```bash
docker exec truthgate truthgate-kubo-status
```

The report includes:

- peer ID and connected peer count
- effective routing, provide, swarm, and storage configuration
- configured and active listen addresses
- public append-announcement addresses
- repository statistics
- AutoNAT observations
- sweep-provider statistics

Useful direct checks include:

```bash
docker exec truthgate ipfs config Routing.Type
docker exec truthgate ipfs config --json Addresses.Swarm
docker exec truthgate ipfs config --json Addresses.AppendAnnounce
docker exec truthgate ipfs provide stat
docker exec truthgate ipfs stats dht
```

A real external validation should use another IPFS node:

```bash
ipfs routing findpeer <truthgate-peer-id>
```

Then add a unique file through TruthGate, obtain its CID, and retrieve it from
the separate node. That verifies routing and content providing, not merely
local daemon health.

## Development

Development uses the same Dockerfile and base Compose service:

```bash
docker compose -f compose.yaml -f compose.dev.yaml up --build
```

The override changes the image target to the .NET SDK development stage,
bind-mounts the repository at `/workspace`, adds persistent NuGet caches, and
runs the web project with `dotnet watch`. TruthGate is available at
`http://localhost:8080` by default.

Kubo initialization, server defaults, repository migrations, persistent paths,
startup order, and process supervision are shared with production. This
prevents development from silently using a different node layout.

Rider can use `compose.yaml` followed by `compose.dev.yaml` directly in a Docker
Compose run configuration. The source directory is mounted directly, so normal
edits trigger `dotnet watch`.

## General configuration

Ordinary host-facing settings live in `.env`. Important values include:

- `TRUTHGATE_IMAGE`
- `KUBO_VERSION`
- `TRUTHGATE_HTTP_PORT`
- `TRUTHGATE_HTTPS_PORT`
- `IPFS_SWARM_PORT`
- `TRUTHGATE_DATA_HOST_PATH`
- `IPFS_REPO_HOST_PATH`
- `IPFS_BLOCKS_HOST_PATH`

Use absolute host paths for advanced deployments. Relative defaults are rooted
at the directory containing `compose.yaml`.

## Process model

`tini` is PID 1 in normal Docker execution. Rider's debugger may become PID 1,
in which case Tini registers as a child subreaper. The entrypoint starts Kubo
first, waits for its RPC API, then starts TruthGate. If either process exits,
the entrypoint terminates the other and exits so Docker's restart policy can
recover the appliance. SIGTERM and SIGINT are forwarded to both processes for
graceful shutdown.
