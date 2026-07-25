# Updating and image tags

## Update a stable deployment

```bash
docker compose pull
docker compose up -d
```

Mounted state remains in place while the container is replaced.

## Published tags

Moving channels:

```text
latest
stable
0.1
```

Immutable release identifiers:

```text
0.1.0
0.1.1
sha-<commit>
```

`latest` and `stable` point to the newest successful stable release. The series tag points to the newest release in that major/minor line.

## Pin an exact release

```env
TRUTHGATE_IMAGE=magiccodingman/truthgate-ipfs:0.1.0
```

This avoids automatic movement when `stable` is updated.

## Automatic version allocation

A successful promotion to the `stable` Git branch:

1. runs the release gates;
2. allocates or reuses a Git tag such as `v0.1.0`;
3. publishes AMD64 and ARM64 manifests;
4. updates version, series, `stable`, `latest`, and SHA tags;
5. verifies the remote manifest;
6. synchronizes the root README to Docker Hub.

`VERSION` contains the intentional major/minor series. Patch numbers increment automatically.

## Before updating

- review release notes and the PR;
- back up persistent state;
- record the current image tag;
- check available disk space.

## Roll back

Set the old exact image tag in `.env`:

```env
TRUTHGATE_IMAGE=magiccodingman/truthgate-ipfs:0.1.0
```

Then:

```bash
docker compose pull
docker compose up -d
```

A software rollback cannot always reverse a Kubo repository migration. Keep a pre-update backup when crossing Kubo repository versions.
