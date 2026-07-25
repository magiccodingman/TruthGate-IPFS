# TGP client resolution

## Minimal algorithm

```javascript
async function resolveTgp(baseUrl) {
  const response = await fetch(new URL("tgp.json", baseUrl), {
    cache: "no-store"
  });

  if (!response.ok) {
    throw new Error(`TGP fetch failed: ${response.status}`);
  }

  const pointer = await response.json();

  if (pointer?.tgp !== 1) {
    throw new Error("Unsupported TGP version");
  }

  if (typeof pointer.current !== "string" || pointer.current.length === 0) {
    throw new Error("Missing TGP current target");
  }

  if (pointer.current.startsWith("/ipfs/")) {
    return pointer.current;
  }

  return `/ipfs/${pointer.current}`;
}
```

Production clients should also validate the CID/path and handle IPNS/gateway trust explicitly.

## Browser redirect

A browser helper can use `location.replace` after validating the pointer.

Avoid open redirects. TGP v1 `current` is an IPFS target, not an arbitrary URL.

## Last-known-good behavior

A client MAY retain a previously validated target. When the current pointer cannot be fetched:

- do not silently claim the old target is current;
- expose stale state where the UI permits;
- apply a bounded retention policy;
- retry with backoff.

## Trust

TGP inherits the trust model of the IPNS identity and the resolution path.

A client using a public HTTP gateway also trusts that gateway to return the resolved content correctly. A local IPFS node reduces reliance on that HTTP gateway but does not change the identity being resolved.

## Caching

Use short caching for `tgp.json`. Cache the immutable target normally.

A client that polls should use conditional requests or a reasonable interval rather than creating unnecessary load.
