# Networking

## Published ports

| Purpose | Protocol | Default host port | Container port |
|---|---:|---:|---:|
| ACME HTTP and optional redirects | TCP | `80` | `80` |
| HTTPS portal and sites | TCP | `443` | `443` |
| Kubo swarm | TCP | `4001` | `4001` |
| Kubo QUIC/WebTransport swarm | UDP | `4001` | `4001` |

Development replaces the HTTP/HTTPS publishing with host port `8080` to container port `80`, while retaining swarm TCP and UDP.

## Firewalls and provider rules

Allow:

```text
80/tcp
443/tcp
4001/tcp
4001/udp
```

Replace `4001` when `IPFS_SWARM_PORT` is customized.

## NAT

When the host is behind NAT, forward both TCP and UDP for the selected swarm port. Forward HTTP and HTTPS when the portal or mapped domains must be reachable publicly.

## Public announce addresses

TruthGate can discover public IPv4 and IPv6 addresses and manage Kubo `Addresses.AppendAnnounce`.

```env
TRUTHGATE_KUBO_PUBLIC_IPV4=auto
TRUTHGATE_KUBO_PUBLIC_IPV6=auto
TRUTHGATE_KUBO_ANNOUNCE_PORT=4001
```

Use `off` to disable one family or set a literal address when auto-detection is inappropriate.

## Internal Kubo listeners

Inside the container:

```text
RPC API:  /ip4/127.0.0.1/tcp/5001
Gateway:  /ip4/127.0.0.1/tcp/9010
```

These are intentionally not published by Compose.

## Reverse proxies

TruthGate is designed to terminate TLS itself. An upstream proxy is possible, but it must preserve the original host and protocol and correctly forward ACME traffic.

Do not add another proxy merely because the application uses Kestrel. Kestrel is the intended public server in the standard deployment.

## Validate a mapped host locally

```bash
curl -kI \
  --resolve example.com:443:127.0.0.1 \
  https://example.com/
```

## Validate swarm reachability

Inside:

```bash
docker exec truthgate ipfs id
```

Outside, from another node:

```bash
ipfs routing findpeer <PEER_ID>
```

An address appearing in local configuration does not prove that the internet can reach it.
