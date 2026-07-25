# Rate-limit defaults

These values describe current application defaults. Operators and future releases may change them.

## Public endpoints

| Previous-hour global traffic | Per-IP budget |
|---:|---:|
| less than `2,000` | `300/min` |
| at least `2,000` | `200/min` |
| at least `8,000` | `100/min` |
| at least `16,000` | `30/min` |

## Gateway routes

| Setting | Default |
|---|---:|
| free per minute per IP | `400` |
| sliding hourly overage | `3,200` |
| ban when overage is exhausted | `4 hours` |
| authenticated auto-whitelist | enabled |

## Response model

- throttled request: normally `429`;
- banned client: `403`;
- invalid or missing key: `401`.

Responses should not disclose remaining quota, ban duration, or whether a supplied key exists.

## Operational warning

Rate limits protect application resources. They are not sufficient for volumetric DDoS defense.
