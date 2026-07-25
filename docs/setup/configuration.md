# Configuration

TruthGate configuration has three layers.

## 1. Compose and `.env`

Host-facing values belong in `.env`:

- image tag;
- host ports;
- persistent host paths;
- build versions;
- certificate mode;
- selected Kubo overrides.

See [Environment variables](../reference/environment-variables.md).

## 2. Persistent Kubo settings

On first boot TruthGate creates:

```text
data/truthgate/config/kubo-settings.json
```

This stores the managed Kubo policy. Environment variables take precedence over it.

Changing the file requires a container restart for current settings to be reapplied.

## 3. Advanced Kubo overrides

Advanced values can be placed in:

```text
data/truthgate/config/kubo-overrides.json
```

Example:

```json
{
  "Swarm.ConnMgr.LowWater": 100,
  "Swarm.ConnMgr.HighWater": 300,
  "Routing.AcceleratedDHTClient": false
}
```

Overrides run after the normal managed defaults.

`Addresses.API` and `Addresses.Gateway` are protected and cannot be changed through this file. TruthGate relies on those listeners remaining loopback-only.

## Empty environment values

The Compose file passes optional variables as empty strings when they are unset. Startup logic treats an empty value as “use the persistent setting or default,” not as a literal Kubo value.

## Certificate settings

```env
TRUTHGATE_ACME_STAGING=false
TRUTHGATE_CERT_IPS=
```

Use ACME staging for repeated certificate testing.

`TRUTHGATE_CERT_IPS` is a comma-separated override for the IP addresses included in the self-signed fallback certificate.

## Configuration ownership

Do not edit generated application configuration while TruthGate is actively writing it. Back up the file first and stop the container for manual recovery work.
