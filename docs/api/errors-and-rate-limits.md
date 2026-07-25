# API errors and rate limits

## Common status codes

| Status | Meaning |
|---:|---|
| `400` | Invalid request or host |
| `401` | Missing or invalid authentication |
| `403` | Access denied or client banned |
| `404` | Route, domain, site, or content not found |
| `429` | Rate limit exceeded |
| `500` | Internal failure |
| `502` / `503` | Upstream Kubo or temporary service failure |

Error bodies should avoid revealing credential validity, ban duration, internal paths, or private configuration.

## Public-controller defaults

The default public per-IP minute budget starts at `300` and tightens with global traffic tiers:

| Global protected requests in the previous hour | Per-IP minute budget |
|---:|---:|
| below `2,000` | `300` |
| `2,000` or more | `200` |
| `8,000` or more | `100` |
| `16,000` or more | `30` |

## Gateway defaults

| Setting | Default |
|---|---:|
| Free requests per minute per IP | `400` |
| Sliding hourly overage | `3,200` |
| Ban after overage exhaustion | `4 hours` |
| Auto-whitelist after authenticated use | enabled |

These are application defaults, not a promise that every deployment has unchanged values.

## Retry behavior

A `429` client should stop, honor `Retry-After` when present, and retry with backoff.

Do not aggressively retry `401` or `403` responses.
