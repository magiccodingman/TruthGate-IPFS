# API documentation

TruthGate has more than one API surface.

## Authenticated Kubo proxy

TruthGate exposes supported Kubo RPC calls through `/api/v0/` while keeping Kubo's local RPC listener private.

Read [Kubo proxy](kubo-proxy.md) and [Authentication](authentication.md).

## Public domain metadata

Published domains expose read-only metadata about the current CID and IPNS/TGP state.

Read [Domain metadata](domain-metadata.md).

## TruthGate application APIs

The management UI and internal services use TruthGate-specific endpoints. These are not all guaranteed as a stable public integration contract.

Publicly documented integrations should be limited to routes that are intentionally supported and tested. When a new endpoint becomes part of the external contract, document:

- method and path;
- authentication;
- request schema;
- response schema;
- errors;
- rate policy;
- compatibility guarantees.

## Errors and limits

Read [Errors and rate limits](errors-and-rate-limits.md).
