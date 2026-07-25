# TGP FAQ

## Why not point IPNS directly at the full site?

You can. TGP is useful when clients and gateways benefit from a tiny, explicit current-pointer document and a narrow mutable contract.

## Does TGP delete old content?

No. It supports updating the pointer, unpinning locally, and garbage collecting local blocks. Other nodes may retain copies.

## Is `tgp` version `1` or `1.1`?

The wire value is integer `1`. Documentation revisions do not change the JSON wire version.

## Does `tgp.json` need its own signature?

IPNS already authenticates publication through its signed record. TGP v1 does not add a second signature field.

## Can I include extra fields?

Yes. V1 clients must ignore unknown fields.

## Can `current` be an HTTP URL?

No. V1 defines a CID or `/ipfs/` path. Allowing arbitrary URLs would create redirect and trust problems.

## Is `index.html` required?

No. It is an optional browser helper.

## Is `legal.md` required?

No. It is optional and informational. Its presence must not block resolution.

## Can I keep deployment history?

Yes, but TGP does not define a public history list. Keep audit history outside the pointer.

## Does it work with IPFS Companion?

A browser redirect to `/ipfs/<cid>` can be handled by an IPFS-aware browser environment or ordinary gateway routing.
