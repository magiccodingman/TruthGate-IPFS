# Migrating from a legacy installation

This guide covers moving a previous bare-metal or separate-Kubo deployment into the current appliance.

## Do not initialize over existing data

The important inputs are:

- TruthGate configuration and database;
- certificates and secrets;
- Kubo repository;
- Kubo blocks;
- IPNS keys.

Back up the legacy system before copying anything.

## Stop legacy services

Stop the old TruthGate service and Kubo daemon so files stop changing.

Examples may include:

```bash
sudo systemctl stop truthgate
sudo systemctl stop ipfs
```

Use the actual service names on the old host.

## Prepare appliance paths

```bash
mkdir -p \
  data/truthgate \
  data/ipfs/repo \
  data/ipfs/blocks
```

Copy the existing data into the corresponding paths.

The Kubo repository must still see its blockstore at `repo/blocks`; the Compose definition mounts the separate host block directory at that location.

## Repository migration

Startup reads the existing repository version. When it is older than the format required by the bundled Kubo, TruthGate runs the repository migrator before applying managed configuration.

If the repository version is newer than the bundled Kubo supports, startup stops instead of attempting a downgrade.

## Managed settings

The appliance applies its server defaults after migration. Review:

- `data/truthgate/config/kubo-settings.json`
- `data/truthgate/config/kubo-overrides.json`
- `.env`

The server profile is tracked so it is not blindly treated as a new repository on every start.

## Identity validation

Before migration:

```bash
ipfs id -f='<id>\n'
```

After migration:

```bash
docker exec truthgate ipfs id -f='<id>\n'
```

The peer ID should match when the original repository and identity were preserved.

## Remove obsolete deployment pieces

After validation, retire:

- the old systemd TruthGate service;
- the old Kubo daemon service or separate container;
- direct public bindings for Kubo RPC and gateway ports;
- shared default credentials;
- auto-update scripts that pull source at service start.

Do not remove backups until the appliance has passed external retrieval, domain, IPNS, and login tests.
