# TGP rationale

## The deliberate trade

An application can use IPNS directly:

```text
IPNS → application CID
```

TGP chooses:

```text
IPNS → small control bundle → application CID
```

That is one additional content-resolution hop. TGP exists because the control layer created by that hop is useful enough to justify the cost.

## What IPNS already provides

IPNS is a signed, mutable pointer to a path. It can be updated to a new CID without replacing the identity. IPNS does not inherently require an operator to keep every previous target pinned or publish a browsable deployment history.

Kubo already provides the storage operations needed to remove eligible local content: pins and references can be removed, and repository garbage collection can remove unprotected blocks.

TGP does not claim those capabilities as inventions of the protocol.

## The pointer model

The IPNS identity points to a small directory containing:

```text
/tgp.json       required
/index.html     optional browser resolver and fallback
```

`tgp.json` gives software a known place to discover the current immutable application CID. `index.html` can make the IPNS location useful to a person rather than exposing only raw JSON.

TruthGate's browser fallback can resolve the pointer, recognize an IPFS-aware browser environment, try a configured Web2 route, navigate to a direct CID path, and provide recovery links when automatic navigation is blocked.

## Primary benefits

### Lightweight control-plane reads

A monitor or client can fetch and validate a small pointer document rather than treating the complete application root as the control record.

### Stable browser landing point

The IPNS identity remains a useful location of its own. The control bundle can direct ordinary browsers, IPFS-aware browsers, and fallback paths toward the current deployment.

### Application-independent routing

Routing and fallback behavior can evolve without embedding that logic into every application build. The pointer bundle provides a natural seam for future destination, transport, gateway, or capability metadata while the application CID remains immutable.

### Explicit retention policy

Direct IPNS does not prevent removal, but it also does not define an application-level retention convention.

TGP gives TruthGate a stable lifecycle boundary around which an operator can choose policies such as:

- current target only;
- the most recent bounded number of deployments;
- selected tagged releases;
- a separate archival policy;
- no public history catalog while audit records remain elsewhere.

The protocol does not perform unpinning or garbage collection by itself. TruthGate or another publisher must implement the selected policy correctly.

### Immutable application caching

After resolution, `/ipfs/<current>` remains ordinary immutable content and can use normal IPFS and HTTP caching.

### Clear current target

Clients do not need to infer which object in the application directory represents the active deployment.

## Costs and limits

- Resolution requires one additional IPFS object.
- Pointer freshness and application caching require different policies.
- A malformed or unavailable pointer can block resolution even when the application CID still exists.
- Updating the pointer does not remove older content from the local repository.
- Local removal requires clearing every retaining pin or reference and allowing garbage collection to remove eligible blocks.
- Independent nodes may retain older CIDs.

## Why IPNS still matters

TGP uses IPNS as the authenticated mutable identity. It does not replace IPNS signing, sequence handling, routing, or name resolution; it defines the payload clients expect after IPNS resolution.
