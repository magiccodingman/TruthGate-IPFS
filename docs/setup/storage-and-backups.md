# Storage and backups

## Persistent host layout

Default:

```text
data/
├── truthgate/
│   ├── certificates/
│   ├── config/
│   ├── database/
│   ├── secrets/
│   └── state/
└── ipfs/
    ├── repo/
    └── blocks/
```

The mounts are:

| Host path | Container path |
|---|---|
| `TRUTHGATE_DATA_HOST_PATH` | `/data/truthgate` |
| `IPFS_REPO_HOST_PATH` | `/data/ipfs/repo` |
| `IPFS_BLOCKS_HOST_PATH` | `/data/ipfs/repo/blocks` |

The separate blockstore mount allows large block storage to move independently without changing Kubo's logical repository layout.

## What must be backed up

Back up all three persistent paths together.

Important state includes:

- TruthGate configuration and database;
- user and API-key records;
- certificates and ACME account state;
- ASP.NET data-protection keys;
- Kubo identity and configuration;
- IPNS keys;
- repository metadata;
- blocks;
- bootstrap and migration state.

Backing up only the blockstore is not a complete backup.

## Consistent backup

For the simplest consistent snapshot:

```bash
docker compose stop truthgate
```

Take the filesystem or volume snapshot, then:

```bash
docker compose start truthgate
```

Storage systems with application-consistent snapshots may support more advanced approaches, but they must preserve the relationship among application state, repository metadata, and blocks.

## Restore

1. Stop TruthGate.
2. Restore all persistent paths.
3. Confirm ownership is compatible with the configured UID and GID.
4. Start the container.
5. Inspect migration and startup logs.
6. Verify the peer ID and published content.

## Storage policy

Default Kubo storage is `auto`, calculated as a percentage of the filesystem containing the blockstore.

```env
TRUTHGATE_KUBO_STORAGE_MAX=auto
TRUTHGATE_KUBO_STORAGE_PERCENT=90
TRUTHGATE_KUBO_STORAGE_FALLBACK=200GB
TRUTHGATE_KUBO_STORAGE_GC_WATERMARK=90
TRUTHGATE_KUBO_ENABLE_GC=true
```

`Datastore.StorageMax` is a garbage-collection policy threshold, not a filesystem quota. Leave headroom for repository metadata, temporary files, and other host workloads.

## Moving the blockstore

1. Stop the container.
2. Copy the current blocks directory while preserving contents.
3. Change `IPFS_BLOCKS_HOST_PATH`.
4. Start the container.
5. Verify repository statistics and sample content.
6. Remove the old copy only after validation.
