# TGP rationale

## The problem

A static application is naturally represented by an immutable CID. Users and applications still need a stable name that can move to a new build.

IPNS provides the signed mutable identity. A publisher can point that identity directly at the complete site, but clients that only need to discover the current deployment must resolve the entire mutable root conceptually as the application.

TGP narrows that mutable surface.

## The pointer model

The IPNS identity points to a small directory containing:

```text
/tgp.json
/index.html    optional
/legal.md      optional
```

`tgp.json` points to the current immutable site CID.

## Operational benefits

### Light mutable payload

The pointer is small and cheap to fetch, validate, mirror, and monitor.

### Immutable application caching

After resolution, `/ipfs/<current>` can use ordinary immutable-content caching.

### Clear current target

Clients do not need to infer which object in a directory is the deployment.

### Removal workflow

An operator can:

1. update the pointer;
2. unpin an old target locally;
3. run repository garbage collection;
4. stop intentionally advertising that target through the IPNS identity.

This controls the operator's own node and pointer. It does not control independent third-party copies.

### No default history catalog

TGP does not define a historical list. Audit records can exist elsewhere without becoming part of the public pointer contract.

## Why IPNS still matters

TGP does not replace IPNS signing, sequence handling, routing, or name resolution. It uses IPNS as the authenticated mutable identity and defines the payload clients expect after resolution.
