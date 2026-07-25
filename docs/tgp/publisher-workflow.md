# TGP publisher workflow

## Inputs

- an IPNS key controlled by the publisher;
- the current site root CID;
- optional domain name;
- optional legal notice.

## Build the pointer directory

```text
tgp-root/
├── tgp.json
├── index.html    optional
└── legal.md      optional
```

Example `tgp.json`:

```json
{
  "tgp": 1,
  "ts": "2026-07-25T17:00:00Z",
  "current": "bafy...",
  "domainName": "example.com",
  "legal": "/legal.md"
}
```

## Publish

1. Validate the site CID.
2. Generate `tgp.json`.
3. Add the pointer directory to IPFS.
4. Publish the resulting pointer-directory CID through the IPNS key.
5. Verify IPNS resolution.
6. Fetch and validate `tgp.json`.
7. Fetch the current site CID.
8. Verify mapped-domain metadata.

## Updating

For a new deployment:

1. publish the new site and obtain its CID;
2. update `current` and `ts`;
3. add the new pointer directory;
4. update IPNS;
5. wait for resolution;
6. verify clients;
7. apply old-target retention policy.

## Removing old content locally

When policy permits:

```bash
ipfs pin rm <OLD_CID>
ipfs repo gc
```

Only run garbage collection with a complete understanding of other pins and repository users.

This removes eligible blocks from the local node. It cannot remove blocks pinned or cached elsewhere.

## Audit history

Keep deployment history in an operator database, Git repository, logging system, or other off-pointer record. TGP does not define a public history array.
