# Development Compose model

TruthGate uses two Compose files for development:

```text
compose.yaml
compose.dev.yaml
```

They are passed in that order:

```bash
docker compose \
  -f compose.yaml \
  -f compose.dev.yaml \
  up --build
```

## Merge behavior

`compose.yaml` defines the production service:

- production build target;
- stable image;
- ports;
- persistent paths;
- environment;
- security options;
- health check.

`compose.dev.yaml` merges into that service and changes development-specific behavior:

- development build target;
- `development` command;
- no restart policy;
- writable root filesystem;
- repository mounted at `/workspace`;
- `dotnet watch`;
- NuGet cache volumes;
- HTTP portal on host `8080`;
- development environment names.

The override uses Compose `!override` where a list must replace rather than append to the production list.

## Where new configuration belongs

Add a new production requirement to `compose.yaml`.

Examples:

- new persistent mount;
- new required environment variable;
- new production port;
- new health-check dependency;
- new container capability.

Add only the development difference to `compose.dev.yaml`.

A development-only addition that never reaches `compose.yaml` is easy to forget when opening a PR and can create a feature that works only on a developer machine.

## Validate merges

```bash
docker compose -f compose.yaml config
```

```bash
docker compose \
  -f compose.yaml \
  -f compose.dev.yaml \
  config
```

Review the merged result when changing ports, lists, volumes, or security settings.

## Stop

```bash
docker compose \
  -f compose.yaml \
  -f compose.dev.yaml \
  down
```

Do not add `-v` unless deleting the development volumes and data is intentional.
