# Persistence

TruthGate treats the container image as disposable and the mounted data as the appliance identity.

## TruthGate state

`/data/truthgate` includes:

- configuration;
- database;
- certificates;
- ACME account data;
- secrets and data-protection keys;
- first-run and migration state.

## Kubo repository

`/data/ipfs/repo` includes:

- peer identity;
- Kubo configuration;
- datastore metadata;
- keystore and IPNS keys;
- repository version;
- logical `blocks` path.

## Blockstore

`/data/ipfs/repo/blocks` is mounted separately from the host path configured by `IPFS_BLOCKS_HOST_PATH`.

## Replacement

`docker compose up -d` can replace the container while preserving the same mounts. The new process sees the existing identity and state.

## Migration

Repository format migration changes persistent data. A container rollback does not necessarily downgrade that data. Back up before updates that change Kubo repository versions.

## Permissions

The appliance runs managed processes as UID/GID `1000` by default. Startup prepares mount roots, but a manually restored tree may still need ownership correction.
