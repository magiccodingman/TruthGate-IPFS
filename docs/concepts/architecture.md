# Architecture

TruthGate is an edge and management layer around Kubo.

## Components

### TruthGate application

The ASP.NET application provides:

- account and session authentication;
- API-key management;
- Blazor management UI;
- TLS and certificate selection;
- domain routing;
- publishing and pinning orchestration;
- request protection;
- metadata endpoints;
- Kubo proxy endpoints.

### Kubo

Kubo provides:

- content addressing;
- block storage and exchange;
- DHT routing;
- IPNS publication and resolution;
- repository and key management;
- the native WebUI;
- local RPC and gateway services.

### Container entrypoint

The entrypoint:

1. creates required directories;
2. validates paths;
3. handles the first-run administrator password;
4. migrates an existing Kubo repository;
5. applies managed Kubo settings;
6. starts Kubo;
7. waits for the loopback RPC API;
8. starts TruthGate;
9. supervises both processes.

If either managed process exits, the entrypoint stops the other and exits so Docker can restart the appliance.

## Trust boundary

Only TruthGate is published as the HTTP edge.

Kubo RPC and gateway listeners remain on loopback. TruthGate calls them from inside the container and decides which behavior is available to an authenticated operator, API key, mapped site domain, or public metadata request.

## Application rendering

The management application uses interactive Blazor Server and WebAssembly components. The production publish must include the `blazor.web.js` bootstrap asset; CI validates that the deployed asset is nontrivial.

## Data boundary

The image is disposable. Persistent state lives under `/data/truthgate` and `/data/ipfs`.

See [Persistence](persistence.md).
