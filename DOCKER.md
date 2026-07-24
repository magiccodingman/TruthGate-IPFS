# TruthGate Docker appliance

TruthGate ships as one container containing the TruthGate ASP.NET application,
Kubo, and the matching `ipfs` CLI. Docker owns the software lifecycle; mounted
state survives image replacement.

## Requirements

- Docker Engine with the Compose v2 plugin
- Docker Compose 2.24.4 or newer for the development override's `!override`
  merge directive
- TCP ports 80, 443, and 4001 plus UDP port 4001 available by default

## Production quick start

```bash
cp .env.example .env
docker compose up --build -d
```

Open `https://localhost` (the first connection uses TruthGate's self-signed
fallback certificate unless a configured domain has an issued certificate).

On the first boot, retrieve the generated administrator password with:

```bash
docker compose logs truthgate
```

The username is `admin`. Change its password in the TruthGate UI. Until the
configuration is persisted, the bootstrap password is also retained at
`data/truthgate/state/bootstrap-admin-password` with restrictive permissions.

## Pulling the published image

After the multi-platform image has been published by GitHub Actions:

```bash
docker compose pull
docker compose up -d
```

The same `master` tag resolves to the correct `linux/amd64` or `linux/arm64`
image automatically. Replacing the container does not replace mounted state.

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
is included in this initial implementation.

The software inside the image is disposable. Backups should target the mounted
state, not the .NET runtime, Kubo binary, application binaries, or build output.

## Kubo startup and migrations

The entrypoint initializes Kubo only when `/data/ipfs/repo/config` is absent.
Existing repositories are preserved. Every start uses:

```bash
ipfs daemon --migrate=true
```

This lets Kubo perform supported repository migrations when a newer image is
started against older persistent data. The RPC API listens only on container
loopback at port 5001, and the Kubo HTTP gateway listens only on loopback at
port 9010. Neither is published to the host.

## Development

Development uses the same Dockerfile and base Compose service:

```bash
docker compose -f compose.yaml -f compose.dev.yaml up --build
```

The override changes the image target to the .NET SDK development stage,
bind-mounts the repository at `/workspace`, adds a persistent NuGet cache, and
runs the web project with `dotnet watch`. TruthGate is available at
`http://localhost:8080` by default.

Kubo initialization, repository migrations, persistent paths, startup order,
and process supervision are shared with production. This prevents development
from silently using a different node layout.

Rider can use `compose.yaml` followed by `compose.dev.yaml` directly in a Docker
Compose run configuration. The source directory is mounted directly, so normal
edits trigger `dotnet watch`.

## Configuration

All ordinary host-facing settings live in `.env`. Important values include:

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

`tini` is PID 1. The entrypoint starts Kubo first, waits for its RPC API, then
starts TruthGate. If either process exits, the entrypoint terminates the other
and exits so Docker's restart policy can recover the appliance. SIGTERM and
SIGINT are forwarded to both processes for graceful shutdown.
