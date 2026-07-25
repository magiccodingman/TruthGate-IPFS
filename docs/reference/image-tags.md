# Docker image tags

Repository:

```text
magiccodingman/truthgate-ipfs
```

## Moving tags

| Tag | Meaning |
|---|---|
| `latest` | newest successful stable release; Docker default |
| `stable` | explicit newest successful stable release |
| `0.1` | newest successful release in the `0.1.x` series |

## Immutable identifiers

| Tag | Meaning |
|---|---|
| `0.1.0` | exact semantic release |
| `sha-<commit>` | image built from a specific commit |

## Development channel

The rolling master image is published to GitHub Container Registry:

```text
ghcr.io/magiccodingman/truthgate-ipfs:master
```

It is not the default production image.

## Pull

```bash
docker pull magiccodingman/truthgate-ipfs:stable
```

Omitting a tag pulls `latest`, which intentionally points to the same digest as `stable`.
