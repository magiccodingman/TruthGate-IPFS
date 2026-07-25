# TGP gateway behavior

A gateway that understands TGP can enforce a narrow host-level contract.

## Recommended behavior

For a TGP-backed IPNS host:

1. resolve and validate `tgp.json`;
2. identify the current target;
3. serve or redirect to that target;
4. use short caching for the pointer;
5. use normal immutable caching for the target;
6. expose the optional legal notice;
7. reject malformed pointers rather than guessing.

## Current-target restriction

A gateway MAY restrict the TGP host to the CID advertised by `current`.

This prevents the same host from becoming a general-purpose path to unrelated or previously advertised CIDs.

It does not prevent users from fetching a known old CID through another gateway or node.

## Browser fallback

An optional `index.html` can fetch `tgp.json` and redirect.

The fallback should:

- avoid arbitrary URL redirects;
- identify stale fallback behavior;
- preserve subpaths only when deliberately specified;
- fail clearly when the pointer is invalid.

## Caches

A reverse proxy SHOULD avoid serving a stale pointer for long periods. Immutable site files can receive long-lived caching.

## Errors

Useful gateway failures include:

- unsupported TGP version;
- malformed pointer;
- current target unavailable;
- IPNS resolution failure.

Do not include private configuration paths or keys in error responses.
