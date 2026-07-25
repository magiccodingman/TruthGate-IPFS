# Ports and paths

## Host ports

| Host | Container | Protocol | Purpose |
|---:|---:|---|---|
| `80` | `80` | TCP | HTTP and ACME |
| `443` | `443` | TCP | HTTPS |
| `4001` | `4001` | TCP | IPFS swarm |
| `4001` | `4001` | UDP | QUIC/WebTransport swarm |
| `8080` dev | `80` | TCP | Development portal |

## Internal listeners

| Listener | Address |
|---|---|
| Kubo RPC | `/ip4/127.0.0.1/tcp/5001` |
| Kubo gateway | `/ip4/127.0.0.1/tcp/9010` |

## Container paths

| Path | Purpose |
|---|---|
| `/app` | Published TruthGate application |
| `/workspace` | Development source mount |
| `/data/truthgate/config/config.json` | TruthGate configuration |
| `/data/truthgate/config/kubo-settings.json` | Managed Kubo settings |
| `/data/truthgate/config/kubo-overrides.json` | Advanced Kubo overrides |
| `/data/truthgate/database` | Databases |
| `/data/truthgate/certificates` | Certificates and ACME state |
| `/data/truthgate/state` | Bootstrap and migration state |
| `/data/truthgate/secrets/data-protection-keys` | ASP.NET data-protection keys |
| `/data/ipfs/repo` | Kubo repository |
| `/data/ipfs/repo/blocks` | Kubo blocks |
| `/run/truthgate` | Runtime temporary state |

## Managed MFS paths

TruthGate publishing uses managed MFS areas including:

```text
/production/
/staging/
```

Do not manually reorganize managed publishing paths without understanding the publishing implementation.
