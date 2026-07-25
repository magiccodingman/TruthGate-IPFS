# TGP FAQ

## Why not point IPNS directly at the full site?

You can. Direct IPNS is valid and already supports moving the same identity to a new CID.

TGP adds a small, predictable control bundle for clients, gateways, browser fallback behavior, retention-policy automation, and future routing metadata. It is a trade: one additional content-resolution hop for those capabilities.

## Does TGP make old content removable?

Not by itself. Direct IPNS publishers can already update the name, unpin old content, remove retaining references, and let Kubo garbage-collect eligible local blocks.

TGP gives TruthGate a consistent boundary around which those retention operations can be configured and automated. The actual removal work still belongs to Kubo and the publishing implementation.

## Does updating TGP delete old content?

No. Updating the pointer only changes the advertised current target. Old CIDs may remain pinned, referenced, cached, archived, or retained by other nodes.

## Why make retention explicit?

Different applications need different policies:

- retain only the current deployment;
- keep the last few deployments;
- preserve selected tagged releases;
- maintain a separate complete archive;
- keep operational audit records without publishing a history catalog.

TGP does not force one choice. It provides a stable place for TruthGate to express and enforce the selected lifecycle.

## Is `tgp` version `1` or `1.1`?

The wire value is integer `1`. Documentation revisions do not change the JSON wire version.

## Does `tgp.json` need its own signature?

IPNS already authenticates publication through its signed record. TGP v1 does not add a second signature field.

## Can I include extra fields?

Yes. V1 clients must ignore unknown fields.

## Can `current` be an HTTP URL?

No. V1 defines a CID or `/ipfs/` path. Allowing arbitrary URLs would create redirect and trust problems. Browser routing belongs in the optional helper or a future explicitly versioned extension.

## Is `index.html` required?

No. It is an optional browser helper. Machine clients resolve `tgp.json` directly.

## Can I keep deployment history?

Yes. TGP does not define a public history list. Keep history in a separate operator database, Git repository, logging system, or archive.

## Does it work with IPFS Companion?

Yes. A browser helper can detect an IPFS-aware environment and navigate to a direct `/ipfs/<current>` destination that Companion or another integration can handle.
