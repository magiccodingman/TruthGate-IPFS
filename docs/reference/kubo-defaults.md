# Kubo defaults

TruthGate initializes and manages Kubo as a contributing server.

| Setting | Default |
|---|---|
| `Routing.Type` | `dhtserver` |
| `Swarm.EnableHolePunching` | `true` |
| `Swarm.RelayClient.Enabled` | `true` |
| `Provide.Enabled` | `true` |
| `Provide.Strategy` | `all` |
| `Provide.DHT.Interval` | `22h` |
| `Provide.DHT.SweepEnabled` | `true` |
| `Provide.DHT.ResumeEnabled` | `true` |
| `Datastore.StorageMax` | auto, 90% of blockstore filesystem |
| `Datastore.StorageGCWatermark` | `90` |
| Automatic repository GC | enabled |
| `Addresses.API` | loopback `5001` |
| `Addresses.Gateway` | loopback `9010` |

TruthGate preserves the current Kubo swarm-listener array and appends missing standard listeners instead of replacing the array. This reduces the chance of deleting transports added by a newer Kubo default.

Inspect the effective node:

```bash
docker exec truthgate truthgate-kubo-status
```
