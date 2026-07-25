# Setup and operations

Production deployment is Docker-first.

## Guides

- [Docker installation](docker.md)
- [First run](first-run.md)
- [Networking](networking.md)
- [Configuration](configuration.md)
- [Storage and backups](storage-and-backups.md)
- [Updating](updating.md)
- [Migrating from legacy installations](migrating-from-legacy.md)

## Recommended sequence

1. Install Docker and the Compose plugin.
2. Clone the repository.
3. Copy `.env.example` to `.env`.
4. Choose persistent host paths.
5. Open HTTP, HTTPS, and swarm ports.
6. Start the stable image.
7. Retrieve the generated administrator password.
8. Change the password.
9. Verify Kubo status from inside and outside the host.
10. Configure domains, pins, and publishing.

## Supported image architectures

The stable Docker Hub release is published for:

- `linux/amd64`
- `linux/arm64`

The same image tag selects the correct architecture automatically.
