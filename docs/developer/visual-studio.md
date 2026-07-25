# Visual Studio development setup

**Status: similar workflow expected; full instructions not yet verified.**

TruthGate development is based on:

```text
compose.yaml
compose.dev.yaml
```

with the production file first and the development override second.

Visual Studio supports Docker Compose projects and should be able to launch the same `truthgate` service, but the maintainer has not yet completed and verified an end-to-end Visual Studio configuration.

Until that happens:

- use the command-line Compose workflow; or
- configure Visual Studio using the same file order and service;
- compare the final merged Compose configuration;
- do not submit untested IDE-specific instructions as authoritative documentation.

Contributions are welcome from someone who has verified:

- build target selection;
- source mounting;
- generated-password retrieval;
- debugger attachment;
- hot reload;
- stop/recreate behavior;
- persistent data handling;
- AMD64 and ARM64 limitations.
