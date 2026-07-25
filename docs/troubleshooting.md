# Troubleshooting

Start with the smallest layer that can fail.

## Collect basic state

```bash
docker compose ps
docker compose logs --tail=300 truthgate
docker inspect truthgate \
  --format 'Status={{.State.Status}} Health={{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}} RestartCount={{.RestartCount}}'
docker exec truthgate truthgate-kubo-status
```

## The portal loads but is not interactive

Check the Blazor bootstrap asset:

```bash
curl -skf \
  -H 'Accept-Encoding: identity' \
  https://127.0.0.1/_framework/blazor.web.js \
  -o /tmp/blazor.web.js

wc -c /tmp/blazor.web.js
```

A zero-byte response indicates a broken publish. The production image build contains a regression guard for this asset.

In a browser, inspect the Network panel for:

- `/_framework/blazor.web.js`
- `/_blazor/negotiate`
- a persistent `/_blazor?id=...` connection

## Login works but the page looks anonymous

Perform a hard refresh after login. Confirm the authentication cookie is present and that the browser is using the same scheme, host, and port used during login.

Do not add anonymous bypasses for `/_blazor` merely to hide an authentication problem.

## Port conflicts during development

Development publishes:

- HTTP portal: host `8080` by default
- IPFS swarm TCP: host `4001`
- IPFS swarm UDP: host `4001`

Stop another Kubo/IPFS Desktop node before starting the development appliance, or deliberately change `IPFS_SWARM_PORT`.

```bash
ss -lntup | grep -E ':(4001|8080)\b'
```

## Container is unhealthy

Inspect logs first. Common causes include:

- persistent directories are not writable;
- a configured path is not absolute inside the container;
- an existing Kubo repository is newer than the bundled Kubo supports;
- another process owns a published host port;
- Kubo failed before its RPC API became ready;
- malformed Kubo override JSON;
- a certificate or configuration file is unreadable.

## Domain serves the portal instead of the site

Confirm:

- the Host header matches the configured domain;
- the domain exists in TruthGate configuration;
- DNS points to this server;
- the domain spelling and case normalization are correct;
- the publication exists in the managed site path.

Test the host locally:

```bash
curl -kI --resolve example.com:443:127.0.0.1 https://example.com/
```

## Domain serves the wrong site

Check for:

- wildcard mappings;
- multiple similar domain entries;
- stale publication configuration;
- uppercase/lowercase leaf differences in managed paths;
- redirects that change the host.

## Certificate does not issue

Verify:

```bash
curl -I http://example.com/
```

Port `80` must reach the same TruthGate instance. Confirm firewall rules, provider security groups, NAT, DNS, and any upstream proxy.

Use ACME staging during repeated tests:

```env
TRUTHGATE_ACME_STAGING=true
```

Do not leave staging enabled for a production certificate.

## Node cannot be found externally

Inside TruthGate:

```bash
docker exec truthgate ipfs id
docker exec truthgate ipfs config --json Addresses.Swarm
docker exec truthgate ipfs config --json Addresses.AppendAnnounce
```

From a separate IPFS node and network:

```bash
ipfs routing findpeer <PEER_ID>
```

Confirm TCP and UDP forwarding for the selected swarm port.

## IPNS watch is stale

Check:

- the IPNS name resolves outside TruthGate;
- the watched entry is enabled;
- the background worker is running;
- the per-key cooldown has elapsed;
- the current CID is retrievable;
- TGP metadata is valid when TGP mode is enabled.

IPNS publication and network resolution are not instantaneous. Test the current record independently before restarting services.

## Data disappeared after recreation

TruthGate state is only persistent when host paths or volumes remain mounted.

Check `.env`:

```env
TRUTHGATE_DATA_HOST_PATH=./data/truthgate
IPFS_REPO_HOST_PATH=./data/ipfs/repo
IPFS_BLOCKS_HOST_PATH=./data/ipfs/blocks
```

Do not use `docker compose down -v` when using named volumes that contain data.

## Kubo repository migration fails

Do not delete the repository version file or initialize over the existing directory.

Back up all persistent paths, then inspect:

```bash
cat data/ipfs/repo/version
docker compose logs truthgate
```

See [Migrating from legacy installations](setup/migrating-from-legacy.md).

## Report an issue

Include:

- TruthGate image tag or commit;
- CPU architecture;
- Docker and Compose versions;
- relevant `.env` values with secrets removed;
- container health;
- logs around the failure;
- whether the problem occurs by IP, management domain, mapped domain, CID, or IPNS;
- exact reproduction steps.
