# Docker installation

TruthGate ships as one appliance containing TruthGate, Kubo, and the matching `ipfs` CLI.

## Requirements

- Docker Engine
- Docker Compose v2.24.4 or newer
- a Linux host for the documented production path
- TCP `80`, `443`, and the selected swarm port
- UDP on the selected swarm port

## Install

```bash
git clone https://github.com/magiccodingman/TruthGate-IPFS.git
cd TruthGate-IPFS
cp .env.example .env
```

Review `.env`, especially the persistent host paths.

Pull and start:

```bash
docker compose pull
docker compose up -d
```

Follow startup:

```bash
docker compose logs -f truthgate
```

## Why the repository is cloned

The published container image contains the software. The repository supplies the supported Compose definition, example environment file, documentation, and local build context.

## Production image

By default:

```text
magiccodingman/truthgate-ipfs:stable
```

Use an exact version for a pinned deployment:

```env
TRUTHGATE_IMAGE=magiccodingman/truthgate-ipfs:0.1.0
```

## Local production build

```bash
docker compose up --build -d
```

This is useful when testing local source changes. Normal operators should pull a tested release.

## Verify

```bash
docker compose ps
docker exec truthgate truthgate-kubo-status
```

The container should report healthy after Kubo and TruthGate are ready.

## Security properties of the Compose definition

The production service:

- uses a read-only root filesystem;
- enables `no-new-privileges`;
- runs managed processes as the non-root `truthgate` user;
- places temporary data in bounded `tmpfs` mounts;
- binds Kubo RPC and gateway services to loopback;
- persists application and repository data outside the image.

Docker is a packaging and isolation layer, not a complete host-security policy. Keep Docker and the host patched, restrict administrative access, and back up persistent data.
