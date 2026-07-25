# Security boundaries

TruthGate reduces the need to expose Kubo directly, but it is not a claim of complete security.

## Publicly reachable

In the standard deployment:

- TruthGate HTTP/HTTPS;
- Kubo swarm TCP/UDP.

## Loopback-only inside the appliance

- Kubo RPC API on port `5001`;
- Kubo gateway on port `9010`.

## Authentication modes

### Browser session

Users sign in through the management portal. Session cookies protect interactive management behavior.

### API key

Integrations can use scoped or supported API credentials. Keys are shown once and stored as hashes.

### Anonymous metadata

Selected domain metadata is public by design. It should not expose secrets.

### Mapped site traffic

A public website is not expected to require an operator login merely to read published files. The domain-routing path is separate from the management portal.

## Native WebUI

The native IPFS WebUI is powerful because it can operate the node. TruthGate exposes it through authenticated routing instead of publishing the local Kubo interface.

## TLS

TruthGate terminates TLS. A self-signed certificate is used for IP addresses and unknown hosts; configured domains can receive ACME certificates.

TLS protects transport to TruthGate. It does not change the public nature of a CID shared on IPFS.

## Rate protection

Rate limiting, bans, whitelists, and TLS-churn detection reduce obvious abuse and expensive request patterns. They do not replace:

- host firewalling;
- updates;
- monitoring;
- resource limits;
- upstream DDoS protection;
- secure passwords;
- key rotation;
- backups.

## Forwarded headers

An upstream proxy can affect client IP, scheme, and host interpretation. Only trust forwarded headers from infrastructure you control. The standard deployment avoids this ambiguity by publishing TruthGate directly.
