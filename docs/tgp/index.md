# TruthGate Pointer protocol

The **TruthGate Pointer protocol (TGP)** is a small convention for using an IPNS identity to advertise one current immutable IPFS target.

An IPNS root contains a required `tgp.json` file. Clients read the pointer and then load the CID in `current`.

```json
{
  "tgp": 1,
  "ts": "2026-07-25T17:00:00Z",
  "current": "bafy...",
  "domainName": "example.com",
  "legal": "/legal.md"
}
```

## Why it matters

TGP keeps the mutable object tiny while the application remains immutable and cacheable.

It supports:

- inexpensive freshness checks;
- one current target;
- simple client resolution;
- ordinary caching of the immutable CID;
- unpin and garbage-collection workflows;
- gateway policies that only serve the advertised target;
- clear separation between current deployment and off-pointer audit history.

## What it does not do

TGP cannot:

- erase blocks held by another node;
- revoke a CID globally;
- prevent caches, mirrors, archives, screenshots, or copies;
- guarantee that deleted content becomes undiscoverable;
- provide legal immunity;
- replace legal advice or an operator's compliance obligations.

## Documentation

- [Rationale](rationale.md)
- [Specification v1](specification-v1.md)
- [Client resolution](client-resolution.md)
- [Publisher workflow](publisher-workflow.md)
- [Gateway behavior](gateway-behavior.md)
- [Legal considerations](legal-considerations.md)
- [Legal-notice template](legal-notice-template.md)
- [FAQ](faq.md)

## Version terminology

- **Wire protocol version:** `1`, represented by `"tgp": 1`.
- **Specification revision:** editorial revision of these documents.

A documentation clarification does not require clients to emit `tgp: 1.1`. A future incompatible wire protocol would use a new integer.
