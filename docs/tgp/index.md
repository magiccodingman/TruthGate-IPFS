# TruthGate Pointer protocol

The **TruthGate Pointer protocol (TGP)** is a small convention for publishing a structured control bundle through an IPNS identity. Its required `tgp.json` file identifies one current immutable IPFS target.

```text
IPNS identity
      │
      ▼
TGP control bundle
├── tgp.json      required machine-readable pointer
└── index.html    optional browser resolver and fallback
      │
      ▼
current application CID
```

```json
{
  "tgp": 1,
  "ts": "2026-07-25T17:00:00Z",
  "current": "bafy...",
  "domainName": "example.com"
}
```

## Why use an extra hop?

IPNS can already point directly at an application CID. TGP deliberately adds one content-resolution hop to gain a stable, predictable control layer.

That layer supports:

- a tiny document for inexpensive freshness checks;
- an explicit current application target;
- normal immutable caching after resolution;
- a browser-capable fallback location at the IPNS root;
- consistent behavior for IPFS-aware and ordinary browsers;
- gateway policies based on a known pointer contract;
- explicit current-only, bounded-history, selected-release, or archival retention policies;
- room for future routing and fallback metadata without changing the application DAG.

## Relationship to IPNS and Kubo

TGP does not replace IPNS. IPNS supplies the signed mutable identity, sequence handling, routing, and name resolution.

TGP also does not create deletion or garbage collection. A direct IPNS publisher can already update the name, remove old pins and references, and allow Kubo to garbage-collect eligible local blocks.

TGP's retention benefit is organizational: TruthGate receives a consistent boundary around which retention policy and deployment lifecycle can be expressed, automated, monitored, and verified independently from the application content itself.

## Technical limits

- Updating `tgp.json` does not delete an older CID.
- Removing local content requires handling every pin and reference that retains it.
- Other nodes, gateways, caches, mirrors, or users may retain copies independently.
- TGP does not make CIDs globally revocable.
- The pointer bundle is an additional IPFS object and resolution hop.

## Documentation

- [Rationale](rationale.md)
- [Specification v1](specification-v1.md)
- [Client resolution](client-resolution.md)
- [Publisher workflow](publisher-workflow.md)
- [Gateway behavior](gateway-behavior.md)
- [FAQ](faq.md)

## Version terminology

- **Wire protocol version:** `1`, represented by `"tgp": 1`.
- **Specification revision:** editorial revision of these documents.

A documentation clarification does not require clients to emit `tgp: 1.1`. A future incompatible wire protocol would use a new integer.
