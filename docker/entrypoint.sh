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
: "${TRUTHGATE_KUBO_REPO_VERSION:=18}"

export IPFS_PATH TRUTHGATE_CONFIG_PATH TRUTHGATE_DATABASE_PATH
export TRUTHGATE_CERT_PATH TRUTHGATE_STATE_PATH TMPDIR

require_absolute_path IPFS_PATH "${IPFS_PATH}"
require_absolute_path TRUTHGATE_CONFIG_PATH "${TRUTHGATE_CONFIG_PATH}"
require_absolute_path TRUTHGATE_DATABASE_PATH "${TRUTHGATE_DATABASE_PATH}"
require_absolute_path TRUTHGATE_CERT_PATH "${TRUTHGATE_CERT_PATH}"
require_absolute_path TRUTHGATE_STATE_PATH "${TRUTHGATE_STATE_PATH}"
require_absolute_path TMPDIR "${TMPDIR}"
[[ "${TRUTHGATE_KUBO_REPO_VERSION}" =~ ^[0-9]+$ ]] \
    || fail "TRUTHGATE_KUBO_REPO_VERSION must be a positive integer."

config_directory="$(dirname "${TRUTHGATE_CONFIG_PATH}")"
blocks_directory="${IPFS_PATH}/blocks"
data_protection_directory="/data/truthgate/secrets/data-protection-keys"

: "${TRUTHGATE_KUBO_SETTINGS_PATH:=${config_directory}/kubo-settings.json}"
: "${TRUTHGATE_KUBO_OVERRIDES_PATH:=${config_directory}/kubo-overrides.json}"
require_absolute_path TRUTHGATE_KUBO_SETTINGS_PATH "${TRUTHGATE_KUBO_SETTINGS_PATH}"
require_absolute_path TRUTHGATE_KUBO_OVERRIDES_PATH "${TRUTHGATE_KUBO_OVERRIDES_PATH}"
export TRUTHGATE_KUBO_SETTINGS_PATH TRUTHGATE_KUBO_OVERRIDES_PATH

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

migrate_existing_kubo_repository() {
    local repo_config="${IPFS_PATH}/config"
    local repo_version_file="${IPFS_PATH}/version"
    local current_version migrated_version

    # Fresh repositories are initialized later by truthgate-configure-kubo and
    # already use the format expected by the bundled Kubo binary.
    [[ -s "${repo_config}" ]] || return 0

    [[ -s "${repo_version_file}" ]] \
        || fail "Existing Kubo repository is missing its version file: ${repo_version_file}"

    current_version="$(tr -d '[:space:]' <"${repo_version_file}")"
    [[ "${current_version}" =~ ^[0-9]+$ ]] \
        || fail "Kubo repository version is invalid: '${current_version}'."

    if (( current_version > TRUTHGATE_KUBO_REPO_VERSION )); then
        fail "Kubo repository version ${current_version} is newer than the bundled Kubo supports (${TRUTHGATE_KUBO_REPO_VERSION}). Upgrade the TruthGate image before starting this repository."
    fi

    if (( current_version == TRUTHGATE_KUBO_REPO_VERSION )); then
        log "Kubo repository is already at version ${current_version}."
        return 0
    fi

    log "Migrating Kubo repository from version ${current_version} to ${TRUTHGATE_KUBO_REPO_VERSION} before applying managed configuration."
    as_truthgate ipfs repo migrate --to="${TRUTHGATE_KUBO_REPO_VERSION}"

    migrated_version="$(tr -d '[:space:]' <"${repo_version_file}")"
    [[ "${migrated_version}" == "${TRUTHGATE_KUBO_REPO_VERSION}" ]] \
        || fail "Kubo repository migration completed without producing expected version ${TRUTHGATE_KUBO_REPO_VERSION}; found '${migrated_version}'."

    log "Kubo repository migration completed at version ${migrated_version}."
}

migrate_existing_kubo_repository

bootstrap_password_file="${TRUTHGATE_STATE_PATH}/bootstrap-admin-password"
if [[ ! -s "${TRUTHGATE_CONFIG_PATH}" ]]; then
    if [[ -n "${TRUTHGATE_BOOTSTRAP_ADMIN_PASSWORD:-}" ]]; then
        bootstrap_password="${TRUTHGATE_BOOTSTRAP_ADMIN_PASSWORD}"
    elif [[ -s "${bootstrap_password_file}" ]]; then
        bootstrap_password="$(<"${bootstrap_password_file}")"
    else
        bootstrap_password="$(od -An -N24 -tx1 /dev/urandom | tr -d ' \n')"
        umask 077
        printf '%s' "${bootstrap_password}" >"${bootstrap_password_file}"
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

/usr/local/bin/truthgate-configure-kubo

daemon_args=(daemon --migrate=true)
if [[ "$(<"${TMPDIR}/kubo-enable-gc")" == "true" ]]; then
    daemon_args+=(--enable-gc)
fi

log "Starting Kubo with automatic repository migrations enabled."
as_truthgate ipfs "${daemon_args[@]}" &
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
