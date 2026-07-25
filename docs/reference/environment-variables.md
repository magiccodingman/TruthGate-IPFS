# Environment variables

## Image and build

| Variable | Default | Purpose |
|---|---|---|
| `TRUTHGATE_IMAGE` | `magiccodingman/truthgate-ipfs:stable` | Image pulled by Compose |
| `DOTNET_VERSION` | `10.0` | Docker build input |
| `KUBO_VERSION` | `v0.42.0` | Docker build input |
| `TRUTHGATE_UID` | `1000` | Runtime user ID |
| `TRUTHGATE_GID` | `1000` | Runtime group ID |

## Host ports

| Variable | Default |
|---|---:|
| `TRUTHGATE_HTTP_PORT` | `80` |
| `TRUTHGATE_HTTPS_PORT` | `443` |
| `TRUTHGATE_DEV_HTTP_PORT` | `8080` |
| `IPFS_SWARM_PORT` | `4001` |

## Host paths

| Variable | Default |
|---|---|
| `TRUTHGATE_DATA_HOST_PATH` | `./data/truthgate` |
| `IPFS_REPO_HOST_PATH` | `./data/ipfs/repo` |
| `IPFS_BLOCKS_HOST_PATH` | `./data/ipfs/blocks` |

## Certificates

| Variable | Default | Purpose |
|---|---|---|
| `TRUTHGATE_ACME_STAGING` | `false` | Use ACME staging |
| `TRUTHGATE_CERT_IPS` | empty | Comma-separated fallback-certificate IPs |

## Kubo policy

| Variable | Default behavior |
|---|---|
| `TRUTHGATE_KUBO_APPLY_SERVER_PROFILE` | enabled |
| `TRUTHGATE_KUBO_ENSURE_SWARM_LISTENERS` | enabled |
| `TRUTHGATE_KUBO_ROUTING_TYPE` | `dhtserver` |
| `TRUTHGATE_KUBO_HOLE_PUNCHING` | enabled |
| `TRUTHGATE_KUBO_RELAY_CLIENT` | enabled |
| `TRUTHGATE_KUBO_PROVIDE_ENABLED` | enabled |
| `TRUTHGATE_KUBO_PROVIDE_STRATEGY` | `all` |
| `TRUTHGATE_KUBO_PROVIDE_INTERVAL` | `22h` |
| `TRUTHGATE_KUBO_PROVIDE_SWEEP` | enabled |
| `TRUTHGATE_KUBO_PROVIDE_RESUME` | enabled |
| `TRUTHGATE_KUBO_STORAGE_MAX` | `auto` |
| `TRUTHGATE_KUBO_STORAGE_PERCENT` | `90` |
| `TRUTHGATE_KUBO_STORAGE_FALLBACK` | `200GB` |
| `TRUTHGATE_KUBO_STORAGE_GC_WATERMARK` | `90` |
| `TRUTHGATE_KUBO_ENABLE_GC` | enabled |
| `TRUTHGATE_KUBO_PUBLIC_IPV4` | `auto` |
| `TRUTHGATE_KUBO_PUBLIC_IPV6` | `auto` |
| `TRUTHGATE_KUBO_ANNOUNCE_PORT` | `IPFS_SWARM_PORT` |

Optional variables are passed as empty strings by Compose when unset. The appliance then uses persistent settings or defaults.
