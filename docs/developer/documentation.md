# Documentation contributions

## Structure

Every directory under `docs/` must contain `index.md`.

Use:

- setup pages for tasks;
- concepts pages for mental models;
- API pages for contracts;
- reference pages for exact values;
- developer pages for contributor workflows;
- TGP pages for protocol material.

## Source of truth

Verify exact routes, defaults, environment variables, and limits against current source before documenting them.

Do not copy old website text without checking it.

## Language

Prefer concrete statements:

> Kubo's RPC API is bound to loopback.

Avoid vague guarantees:

> The API is completely secure.

## Planned behavior

Label it explicitly:

> **Planned:** TruthGate may proxy additional swarm transports in a future architecture.

## Prohibited stale strings

Do not reintroduce:

- old organization/repository branding;
- shared default administrator passwords;
- old bare-metal auto-update scripts;
- claims that TruthGate cannot run in Docker;
- outdated runtime or UI-architecture labels as the current architecture;
- legal-immunity or guaranteed-deletion claims.

## Images

Use repository-relative paths to `content/images/demo/`.

Images should explain a nearby feature and include useful alt text.

## Links

Use relative links for repository documentation so they work on GitHub and Docker Hub's synchronized README.
