#!/usr/bin/env bash
set -Eeuo pipefail

mode="${1:-production}"
truthgate_user="truthgate"
truthgate_group="$(id -g "${truthgate_user}")"

log() {
    printf '[truthgate] %s\n' "$*"
}

fail() {
    printf '[truthgate] ERROR: %s\n' "$*" >&2
    exit 1
}

as_truthgate() {
    gosu "${truthgate_user}:${truthgate_group}" "$@"
}

require_absolute_path() {
    local name="$1"
    local value="$2"
    [[ "${value}" = /* ]] || fail "${name} must be an absolute path; received '${value}'."
}

: "${IPFS_PATH:=/data/ipfs/repo}"
: "${TRUTHGATE_CONFIG_PATH:=/data/truthgate/config/config.json}"
: "${TRUTHGATE_DATABASE_PATH:=/data/truthgate/database}"
: "${TRUTHGATE_CERT_PATH:=/data/truthgate/certificates}"
: "${TRUTHGATE_STATE_PATH:=/data/truthgate/state}"
: "${TMPDIR:=/run/truthgate}"

export IPFS_PATH TRUTHGATE_CONFIG_PATH TRUTHGATE_DATABASE_PATH
export TRUTHGATE_CERT_PATH TRUTHGATE_STATE_PATH TMPDIR

require_absolute_path IPFS_PATH "${IPFS_PATH}"
require_absolute_path TRUTHGATE_CONFIG_PATH "${TRUTHGATE_CONFIG_PATH}"
require_absolute_path TRUTHGATE_DATABASE_PATH "${TRUTHGATE_DATABASE_PATH}"
require_absolute_path TRUTHGATE_CERT_PATH "${TRUTHGATE_CERT_PATH}"
require_absolute_path TRUTHGATE_STATE_PATH "${TRUTHGATE_STATE_PATH}"
require_absolute_path TMPDIR "${TMPDIR}"

config_directory="$(dirname "${TRUTHGATE_CONFIG_PATH}")"
blocks_directory="${IPFS_PATH}/blocks"
data_protection_directory="/data/truthgate/secrets/data-protection-keys"

install -d -m 0750 -o "${truthgate_user}" -g "${truthgate_group}" \
    "${config_directory}" \
    "${TRUTHGATE_DATABASE_PATH}" \
    "${TRUTHGATE_CERT_PATH}" \
    "${TRUTHGATE_STATE_PATH}" \
    "${data_protection_directory}" \
    "${IPFS_PATH}" \
    "${blocks_directory}" \
    "${TMPDIR}"

# Named volumes are created as root-owned directories. Development tools such as
# NuGet run as the non-root truthgate user, so initialize the complete NuGet home
# (including its configuration directory) before dropping privileges.
if [[ "${mode}" == "development" ]]; then
    : "${HOME:=/home/truthgate}"
    : "${DOTNET_CLI_HOME:=${HOME}}"
    : "${NUGET_PACKAGES:=${HOME}/.nuget/packages}"
    : "${NUGET_HTTP_CACHE_PATH:=${HOME}/.nuget/http-cache}"

    export HOME DOTNET_CLI_HOME NUGET_PACKAGES NUGET_HTTP_CACHE_PATH

    nuget_root="${HOME}/.nuget"
    nuget_config_directory="${nuget_root}/NuGet"

    install -d -m 0750 -o "${truthgate_user}" -g "${truthgate_group}" \
        "${HOME}" \
        "${nuget_root}" \
        "${nuget_config_directory}" \
        "${NUGET_PACKAGES}" \
        "${NUGET_HTTP_CACHE_PATH}"

    chown "${truthgate_user}:${truthgate_group}" \
        "${HOME}" \
        "${nuget_root}" \
        "${nuget_config_directory}" \
        "${NUGET_PACKAGES}" \
        "${NUGET_HTTP_CACHE_PATH}"
fi

# The default Compose layout mounts the repo and blockstore separately. Changing
# ownership on the mount roots is cheap and avoids recursively walking a large
# existing blockstore on every container start.
chown "${truthgate_user}:${truthgate_group}" \
    "${IPFS_PATH}" \
    "${blocks_directory}" \
    "${config_directory}" \
    "${TRUTHGATE_DATABASE_PATH}" \
    "${TRUTHGATE_CERT_PATH}" \
    "${TRUTHGATE_STATE_PATH}" \
    "${data_protection_directory}" \
    "${TMPDIR}"

bootstrap_password_file="${TRUTHGATE_STATE_PATH}/bootstrap-admin-password"
if [[ ! -s "${TRUTHGATE_CONFIG_PATH}" ]]; then
    if [[ -n "${TRUTHGATE_BOOTSTRAP_ADMIN_PASSWORD:-}" ]]; then
        bootstrap_password="${TRUTHGATE_BOOTSTRAP_ADMIN_PASSWORD}"
    elif [[ -s "${bootstrap_password_file}" ]]; then
        bootstrap_password="$(<"${bootstrap_password_file}")"
    else
        bootstrap_password="$(od -An -N24 -tx1 /dev/urandom | tr -d ' \n')"
        umask 077
        printf '%s' "${bootstrap_password}" > "${bootstrap_password_file}"
        chown "${truthgate_user}:${truthgate_group}" "${bootstrap_password_file}"
    fi

    export TRUTHGATE_BOOTSTRAP_ADMIN_PASSWORD="${bootstrap_password}"
    log "First-run administrator account: admin"
    log "First-run administrator password: ${bootstrap_password}"
    log "The password is retained at ${bootstrap_password_file} until the TruthGate config is persisted."
else
    rm -f "${bootstrap_password_file}"
    unset TRUTHGATE_BOOTSTRAP_ADMIN_PASSWORD || true
fi

if [[ ! -s "${IPFS_PATH}/config" ]]; then
    log "Initializing a new Kubo repository at ${IPFS_PATH}."
    as_truthgate ipfs init
else
    log "Using the existing Kubo repository at ${IPFS_PATH}."
fi

# TruthGate talks to Kubo over loopback. The RPC API is intentionally not
# published by Compose; the gateway is likewise kept private behind TruthGate.
as_truthgate ipfs config Addresses.API /ip4/127.0.0.1/tcp/5001
as_truthgate ipfs config Addresses.Gateway /ip4/127.0.0.1/tcp/9010

log "Starting Kubo with automatic repository migrations enabled."
as_truthgate ipfs daemon --migrate=true &
ipfs_pid=$!

stop_children() {
    trap - TERM INT

    if [[ -n "${app_pid:-}" ]] && kill -0 "${app_pid}" 2>/dev/null; then
        kill -TERM "${app_pid}" 2>/dev/null || true
    fi

    if kill -0 "${ipfs_pid}" 2>/dev/null; then
        kill -TERM "${ipfs_pid}" 2>/dev/null || true
    fi

    [[ -z "${app_pid:-}" ]] || wait "${app_pid}" 2>/dev/null || true
    wait "${ipfs_pid}" 2>/dev/null || true
}

trap 'stop_children; exit 143' TERM
trap 'stop_children; exit 130' INT

kubo_ready=false
for _ in $(seq 1 120); do
    if as_truthgate ipfs --api=/ip4/127.0.0.1/tcp/5001 id >/dev/null 2>&1; then
        kubo_ready=true
        break
    fi

    if ! kill -0 "${ipfs_pid}" 2>/dev/null; then
        wait "${ipfs_pid}" || true
        fail "Kubo exited before its RPC API became ready."
    fi

    sleep 1
done

[[ "${kubo_ready}" == true ]] || fail "Kubo did not become ready within 120 seconds."
log "Kubo is ready."

case "${mode}" in
    production)
        [[ -f /app/TruthGate-Web.dll ]] || fail "Production application was not found at /app/TruthGate-Web.dll."
        log "Starting TruthGate in production mode."
        as_truthgate dotnet /app/TruthGate-Web.dll &
        ;;
    development)
        project="${TRUTHGATE_DEV_PROJECT:-/workspace/TruthGate-Web/TruthGate-Web/TruthGate-Web.csproj}"
        [[ -f "${project}" ]] || fail "Development project was not found at ${project}. Is the repository mounted at /workspace?"
        export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://0.0.0.0:80}"
        export DOTNET_USE_POLLING_FILE_WATCHER="${DOTNET_USE_POLLING_FILE_WATCHER:-1}"
        log "Starting TruthGate with dotnet watch (${project})."
        as_truthgate dotnet watch --project "${project}" run --no-launch-profile --urls "${ASPNETCORE_URLS}" &
        ;;
    *)
        fail "Unknown run mode '${mode}'. Expected 'production' or 'development'."
        ;;
esac

app_pid=$!

set +e
wait -n "${ipfs_pid}" "${app_pid}"
exit_code=$?
set -e

log "A managed process exited with status ${exit_code}; stopping the remaining process."
stop_children
exit "${exit_code}"
