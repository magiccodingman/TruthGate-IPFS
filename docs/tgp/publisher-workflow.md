# TGP publisher workflow

## Inputs

- an IPNS key controlled by the publisher;
- the current site root CID;
- an optional domain name;
- optional browser-routing configuration.

## Build the pointer directory

```text
tgp-root/
├── tgp.json
└── index.html    optional browser resolver and fallback
```

Example `tgp.json`:

```json
{
  "tgp": 1,
  "ts": "2026-07-25T17:00:00Z",
  "current": "bafy...",
  "domainName": "example.com"
}
```

## Publish

1. Validate the site CID.
2. Generate `tgp.json`.
3. Generate the optional browser helper.
4. Add the pointer directory to IPFS.
5. Publish the resulting pointer-directory CID through the IPNS key.
6. Verify IPNS resolution.
7. Fetch and validate `tgp.json`.
8. Test the browser helper in ordinary and IPFS-aware browser contexts.
9. Fetch the current site CID.
10. Verify mapped-domain metadata.

## Updating

For a new deployment:

1. publish the new site and obtain its CID;
2. update `current` and `ts`;
3. build and add the new pointer directory;
4. update IPNS;
5. wait for resolution;
6. verify machine clients and browser behavior;
7. apply the separately configured old-target retention policy.

## Retention is separate from pointer publication

Publishing a new TGP root changes the advertised current target. It does not automatically remove the previous application CID or previous pointer bundle from a Kubo repository.

A complete current-only or bounded-history implementation must account for:

- direct recursive pins;
- parent or indirect pins;
- MFS references;
- watched-IPNS retention;
- other local applications or archives;
- remote pinning services;
- local blockstore garbage collection.

When an old CID is no longer protected by any required reference, Kubo operations may include:

```bash
ipfs pin rm <OLD_CID>
ipfs repo gc
```

Only run garbage collection with a complete understanding of other pins and repository users. These operations affect the local node; they cannot remove copies retained elsewhere.

TGP supplies the stable control boundary for retention policy. Kubo supplies the actual pin and repository operations.

## Deployment history

TGP does not define a public history array. Keep deployment history in an operator database, Git repository, logging system, or separate archive when desired.
