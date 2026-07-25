# Rider development setup

These steps describe the tested JetBrains Rider workflow.

## Create a Docker Compose run configuration

1. Open the TruthGate repository in Rider.
2. Open **Run | Edit Configurations**.
3. Add a **Docker Compose** configuration.
4. Set the service to:

```text
truthgate
```

5. Set the Compose files, in order, to:

```text
./compose.yaml
./compose.dev.yaml
```

Depending on Rider's field presentation, the combined value may appear as:

```text
./compose.yaml; ./compose.dev.yaml;
```

The order matters. Production is the base; development is the override.

6. Enable image build before startup.
7. Run the configuration.

## Open TruthGate

```text
http://localhost:8080
```

## Retrieve the generated password

From Rider's container logs, or:

```bash
docker compose \
  -f compose.yaml \
  -f compose.dev.yaml \
  logs truthgate
```

Find the generated `admin` password.

## Hot reload

The repository is mounted at `/workspace`. The development entrypoint runs `dotnet watch` for:

```text
/workspace/TruthGate-Web/TruthGate-Web/TruthGate-Web.csproj
```

NuGet package and HTTP caches use Docker volumes so normal rebuilds do not redownload everything.

## Debugging considerations

Rider can attach to or launch within the development container depending on local configuration. The important invariant is that the Compose service, mounts, environment, and Kubo instance match the documented development appliance.

## Avoid another local IPFS node

Stop IPFS Desktop or another Kubo daemon before using the default development setup. Swarm TCP and UDP port `4001` conflicts can prevent startup or produce misleading connectivity results.

## Reset development state

Stop the stack, move the local `data/` directory aside, and restart. Do not delete it unless losing the development identity, keys, pins, users, and configuration is intended.
