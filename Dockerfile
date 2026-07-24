# syntax=docker/dockerfile:1.7

ARG DOTNET_VERSION=10.0
ARG KUBO_VERSION=v0.42.0
ARG TRUTHGATE_UID=1000
ARG TRUTHGATE_GID=1000

FROM ipfs/kubo:${KUBO_VERSION} AS kubo

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-resolute AS build
WORKDIR /src

COPY TruthGate-Web/TruthGate-Web/TruthGate-Web.csproj TruthGate-Web/TruthGate-Web/
COPY TruthGate-Web/TruthGate-Web.Client/TruthGate-Web.Client.csproj TruthGate-Web/TruthGate-Web.Client/
RUN dotnet restore TruthGate-Web/TruthGate-Web/TruthGate-Web.csproj

COPY . .
RUN dotnet publish TruthGate-Web/TruthGate-Web/TruthGate-Web.csproj \
    --configuration Release \
    --no-restore \
    --output /out \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-resolute AS runtime-base
ARG TRUTHGATE_UID
ARG TRUTHGATE_GID

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        gosu \
        tini \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --gid "${TRUTHGATE_GID}" truthgate \
    && useradd --uid "${TRUTHGATE_UID}" --gid truthgate --create-home --shell /usr/sbin/nologin truthgate \
    && mkdir -p /home/truthgate/.aspnet \
    && ln -s /data/truthgate/secrets/data-protection-keys /home/truthgate/.aspnet/DataProtection-Keys

COPY --from=kubo /usr/local/bin/ipfs /usr/local/bin/ipfs
COPY --chmod=0755 docker/entrypoint.sh /usr/local/bin/truthgate-entrypoint
COPY --chmod=0755 docker/healthcheck.sh /usr/local/bin/truthgate-healthcheck

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_ENVIRONMENT=Production \
    DOTNET_NOLOGO=true \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true \
    HOME=/home/truthgate \
    IPFS_PATH=/data/ipfs/repo \
    TMPDIR=/run/truthgate \
    TRUTHGATE_CERT_PATH=/data/truthgate/certificates \
    TRUTHGATE_CONFIG_PATH=/data/truthgate/config/config.json \
    TRUTHGATE_DATABASE_PATH=/data/truthgate/database \
    TRUTHGATE_STATE_PATH=/data/truthgate/state \
    TRUTHGATE_HEALTH_URL=https://127.0.0.1:443/

EXPOSE 80 443 4001/tcp 4001/udp

ENTRYPOINT ["/usr/bin/tini", "--", "/usr/local/bin/truthgate-entrypoint"]
HEALTHCHECK --interval=30s --timeout=10s --start-period=90s --retries=3 \
    CMD ["/usr/local/bin/truthgate-healthcheck"]

FROM runtime-base AS production
WORKDIR /app
COPY --from=build /out .
CMD ["production"]

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-resolute AS development
ARG TRUTHGATE_UID
ARG TRUTHGATE_GID

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        git \
        gosu \
        tini \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --gid "${TRUTHGATE_GID}" truthgate \
    && useradd --uid "${TRUTHGATE_UID}" --gid truthgate --create-home --shell /bin/bash truthgate \
    && mkdir -p /workspace /home/truthgate/.aspnet \
    && chown truthgate:truthgate /workspace \
    && ln -s /data/truthgate/secrets/data-protection-keys /home/truthgate/.aspnet/DataProtection-Keys

COPY --from=kubo /usr/local/bin/ipfs /usr/local/bin/ipfs
COPY --chmod=0755 docker/entrypoint.sh /usr/local/bin/truthgate-entrypoint
COPY --chmod=0755 docker/healthcheck.sh /usr/local/bin/truthgate-healthcheck

ENV ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_ENVIRONMENT=Development \
    DOTNET_CLI_HOME=/tmp/dotnet \
    DOTNET_NOLOGO=true \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true \
    HOME=/home/truthgate \
    IPFS_PATH=/data/ipfs/repo \
    TMPDIR=/run/truthgate \
    TRUTHGATE_CERT_PATH=/data/truthgate/certificates \
    TRUTHGATE_CONFIG_PATH=/data/truthgate/config/config.json \
    TRUTHGATE_DATABASE_PATH=/data/truthgate/database \
    TRUTHGATE_DEV_PROJECT=/workspace/TruthGate-Web/TruthGate-Web/TruthGate-Web.csproj \
    TRUTHGATE_STATE_PATH=/data/truthgate/state \
    TRUTHGATE_HEALTH_URL=http://127.0.0.1:80/

WORKDIR /workspace
EXPOSE 80 4001/tcp 4001/udp
ENTRYPOINT ["/usr/bin/tini", "--", "/usr/local/bin/truthgate-entrypoint"]
HEALTHCHECK --interval=30s --timeout=10s --start-period=120s --retries=3 \
    CMD ["/usr/local/bin/truthgate-healthcheck"]
CMD ["development"]
