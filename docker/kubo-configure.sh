#!/usr/bin/env bash
set -Eeuo pipefail

truthgate_user="truthgate"
truthgate_group="$(id -g "${truthgate_user}")"

log() { printf '[truthgate] %s\n' "$*"; }
warn() { printf '[truthgate] WARNING: %s\n' "$*" >&2; }
fail() { printf '[truthgate] ERROR: %s\n' "$*" >&2; exit 1; }
as_truthgate() { gosu "${truthgate_user}:${truthgate_group}" "$@"; }

normalize_bool() {
    case "${1,,}" in
        1|true|yes|on) printf 'true' ;;
        0|false|no|off) printf 'false' ;;
        *) return 1 ;;
    esac
}

json_setting() {
    local filter="$1" fallback="$2" value
    value="$(jq -er "${filter} // empty" "${TRUTHGATE_KUBO_SETTINGS_PATH}" 2>/dev/null || true)"
    [[ -n "${value}" ]] && printf '%s' "${value}" || printf '%s' "${fallback}"
}

setting() {
    local environment_name="$1" filter="$2" fallback="$3"
    local environment_value="${!environment_name:-}"
    [[ -n "${environment_value}" ]] \
        && printf '%s' "${environment_value}" \
        || json_setting "${filter}" "${fallback}"
}

set_string() {
    as_truthgate ipfs config "$1" --json "$(jq -cn --arg value "$2" '$value')"
}

set_json() {
    as_truthgate ipfs config "$1" --json "$2"
}

ensure_array_value() {
    local key="$1" item="$2" current compact updated
    current="$(as_truthgate ipfs config "${key}" --json 2>/dev/null || printf '[]')"
    compact="$(jq -c . <<<"${current}")"
    updated="$(jq -c --arg item "${item}" \
        'if type != "array" then [$item] elif index($item) then . else . + [$item] end' \
        <<<"${current}")"
    [[ "${updated}" == "${compact}" ]] || set_json "${key}" "${updated}"
}

valid_ipv4() {
    local a b c d extra octet
    IFS=. read -r a b c d extra <<<"$1"
    [[ -z "${extra:-}" && -n "${a:-}" && -n "${b:-}" && -n "${c:-}" && -n "${d:-}" ]] || return 1
    for octet in "${a}" "${b}" "${c}" "${d}"; do
        [[ "${octet}" =~ ^[0-9]{1,3}$ ]] || return 1
        (( 10#${octet} <= 255 )) || return 1
    done
}

valid_ipv6() {
    [[ "$1" == *:* && "$1" =~ ^[0-9A-Fa-f:.]+$ ]]
}

resolve_public_address() {
    local family="$1" requested="$2" address=""
    case "${requested,,}" in
        ""|off|none|disabled|false) return 0 ;;
        auto)
            if [[ "${family}" == "4" ]]; then
                address="$(curl -4fsS --max-time 5 https://api.ipify.org 2>/dev/null || true)"
            else
                address="$(curl -6fsS --max-time 5 https://api6.ipify.org 2>/dev/null || true)"
            fi
            address="${address//$'\r'/}"
            address="${address//$'\n'/}"
            ;;
        *) address="${requested}" ;;
    esac

    if [[ -z "${address}" ]]; then
        warn "Could not detect a public IPv${family} address; no managed IPv${family} announce address will be added."
        return 0
    fi

    if [[ "${family}" == "4" ]]; then
        valid_ipv4 "${address}" || fail "Invalid public IPv4 address: '${address}'."
    else
        valid_ipv6 "${address}" || fail "Invalid public IPv6 address: '${address}'."
    fi
    printf '%s' "${address}"
}

human_bytes() {
    command -v numfmt >/dev/null 2>&1 \
        && numfmt --to=iec-i --suffix=B "$1" \
        || printf '%sB' "$1"
}

: "${IPFS_PATH:=/data/ipfs/repo}"
: "${TRUTHGATE_CONFIG_PATH:=/data/truthgate/config/config.json}"
: "${TRUTHGATE_STATE_PATH:=/data/truthgate/state}"
: "${TMPDIR:=/run/truthgate}"

config_directory="$(dirname "${TRUTHGATE_CONFIG_PATH}")"
blocks_directory="${IPFS_PATH}/blocks"
: "${TRUTHGATE_KUBO_SETTINGS_PATH:=${config_directory}/kubo-settings.json}"
: "${TRUTHGATE_KUBO_OVERRIDES_PATH:=${config_directory}/kubo-overrides.json}"
export IPFS_PATH TRUTHGATE_KUBO_SETTINGS_PATH TRUTHGATE_KUBO_OVERRIDES_PATH

if [[ ! -e "${TRUTHGATE_KUBO_SETTINGS_PATH}" ]]; then
    cat >"${TRUTHGATE_KUBO_SETTINGS_PATH}" <<'JSON'
{
  "serverProfile": true,
  "routingType": "dhtserver",
  "ensureSwarmListeners": true,
  "holePunching": true,
  "relayClient": true,
  "provide": {
    "enabled": true,
    "strategy": "all",
    "interval": "22h",
    "sweepEnabled": true,
    "resumeEnabled": true
  },
  "storage": {
    "max": "auto",
    "percent": 90,
    "fallback": "200GB",
    "gcWatermark": 90,
    "enableGc": true
  },
  "publicAnnounce": {
    "ipv4": "auto",
    "ipv6": "auto",
    "port": null
  }
}
JSON
    chown "${truthgate_user}:${truthgate_group}" "${TRUTHGATE_KUBO_SETTINGS_PATH}"
    chmod 0640 "${TRUTHGATE_KUBO_SETTINGS_PATH}"
fi
jq -e 'type == "object"' "${TRUTHGATE_KUBO_SETTINGS_PATH}" >/dev/null \
    || fail "Kubo settings file is not a valid JSON object: ${TRUTHGATE_KUBO_SETTINGS_PATH}"

if [[ ! -e "${TRUTHGATE_KUBO_OVERRIDES_PATH}" ]]; then
    printf '{}\n' >"${TRUTHGATE_KUBO_OVERRIDES_PATH}"
    chown "${truthgate_user}:${truthgate_group}" "${TRUTHGATE_KUBO_OVERRIDES_PATH}"
    chmod 0640 "${TRUTHGATE_KUBO_OVERRIDES_PATH}"
fi
jq -e 'type == "object"' "${TRUTHGATE_KUBO_OVERRIDES_PATH}" >/dev/null \
    || fail "Kubo overrides file is not a valid JSON object: ${TRUTHGATE_KUBO_OVERRIDES_PATH}"

server_profile_raw="$(setting TRUTHGATE_KUBO_APPLY_SERVER_PROFILE '.serverProfile' true)"
server_profile="$(normalize_bool "${server_profile_raw}")" \
    || fail "TRUTHGATE_KUBO_APPLY_SERVER_PROFILE must be true or false."
server_profile_marker="${TRUTHGATE_STATE_PATH}/kubo-server-profile-v1"

if [[ ! -s "${IPFS_PATH}/config" ]]; then
    if [[ "${server_profile}" == "true" ]]; then
        log "Initializing a new Kubo repository with the public-server profile."
        as_truthgate ipfs init --profile server
        printf 'applied during ipfs init\n' >"${server_profile_marker}"
        chown "${truthgate_user}:${truthgate_group}" "${server_profile_marker}"
    else
        log "Initializing a new Kubo repository without the server profile."
        as_truthgate ipfs init
    fi
else
    log "Using the existing Kubo repository at ${IPFS_PATH}."
    if [[ "${server_profile}" == "true" && ! -e "${server_profile_marker}" ]]; then
        log "Applying Kubo's public-server profile to the existing repository."
        as_truthgate ipfs config profile apply server
        printf 'applied to existing repository\n' >"${server_profile_marker}"
        chown "${truthgate_user}:${truthgate_group}" "${server_profile_marker}"
    fi
fi

set_string Routing.Type "$(setting TRUTHGATE_KUBO_ROUTING_TYPE '.routingType' dhtserver)"

ensure_swarm_raw="$(setting TRUTHGATE_KUBO_ENSURE_SWARM_LISTENERS '.ensureSwarmListeners' true)"
ensure_swarm="$(normalize_bool "${ensure_swarm_raw}")" \
    || fail "TRUTHGATE_KUBO_ENSURE_SWARM_LISTENERS must be true or false."
if [[ "${ensure_swarm}" == "true" ]]; then
    for address in \
        /ip4/0.0.0.0/tcp/4001 \
        /ip6/::/tcp/4001 \
        /ip4/0.0.0.0/udp/4001/quic-v1 \
        /ip4/0.0.0.0/udp/4001/quic-v1/webtransport \
        /ip6/::/udp/4001/quic-v1 \
        /ip6/::/udp/4001/quic-v1/webtransport
    do
        ensure_array_value Addresses.Swarm "${address}"
    done
fi

for spec in \
    "TRUTHGATE_KUBO_HOLE_PUNCHING|.holePunching|true|Swarm.EnableHolePunching" \
    "TRUTHGATE_KUBO_RELAY_CLIENT|.relayClient|true|Swarm.RelayClient.Enabled" \
    "TRUTHGATE_KUBO_PROVIDE_ENABLED|.provide.enabled|true|Provide.Enabled" \
    "TRUTHGATE_KUBO_PROVIDE_SWEEP|.provide.sweepEnabled|true|Provide.DHT.SweepEnabled" \
    "TRUTHGATE_KUBO_PROVIDE_RESUME|.provide.resumeEnabled|true|Provide.DHT.ResumeEnabled"
do
    IFS='|' read -r env_name filter fallback key <<<"${spec}"
    raw="$(setting "${env_name}" "${filter}" "${fallback}")"
    value="$(normalize_bool "${raw}")" || fail "${env_name} must be true or false."
    set_json "${key}" "${value}"
done

set_string Provide.Strategy "$(setting TRUTHGATE_KUBO_PROVIDE_STRATEGY '.provide.strategy' all)"
set_string Provide.DHT.Interval "$(setting TRUTHGATE_KUBO_PROVIDE_INTERVAL '.provide.interval' 22h)"

storage_max_requested="$(setting TRUTHGATE_KUBO_STORAGE_MAX '.storage.max' auto)"
storage_percent="$(setting TRUTHGATE_KUBO_STORAGE_PERCENT '.storage.percent' 90)"
storage_fallback="$(setting TRUTHGATE_KUBO_STORAGE_FALLBACK '.storage.fallback' 200GB)"
storage_gc_watermark="$(setting TRUTHGATE_KUBO_STORAGE_GC_WATERMARK '.storage.gcWatermark' 90)"

[[ "${storage_percent}" =~ ^[0-9]+$ ]] && (( storage_percent >= 1 && storage_percent <= 100 )) \
    || fail "TRUTHGATE_KUBO_STORAGE_PERCENT must be an integer from 1 through 100."
[[ "${storage_gc_watermark}" =~ ^[0-9]+$ ]] && (( storage_gc_watermark <= 100 )) \
    || fail "TRUTHGATE_KUBO_STORAGE_GC_WATERMARK must be an integer from 0 through 100."

storage_source="fixed"
if [[ "${storage_max_requested,,}" == "auto" ]]; then
    storage_total_bytes="$(df -PB1 -- "${blocks_directory}" 2>/dev/null | awk 'NR == 2 { print $2 }' || true)"
    if [[ "${storage_total_bytes}" =~ ^[0-9]+$ && "${storage_total_bytes}" -gt 0 ]]; then
        storage_max_effective="$(( storage_total_bytes * storage_percent / 100 ))B"
        storage_source="auto (${storage_percent}% of $(human_bytes "${storage_total_bytes}"))"
    else
        storage_max_effective="${storage_fallback}"
        storage_source="fallback because filesystem capacity detection failed"
    fi
else
    storage_max_effective="${storage_max_requested}"
fi
set_string Datastore.StorageMax "${storage_max_effective}"
set_json Datastore.StorageGCWatermark "${storage_gc_watermark}"

enable_gc_raw="$(setting TRUTHGATE_KUBO_ENABLE_GC '.storage.enableGc' true)"
enable_gc="$(normalize_bool "${enable_gc_raw}")" \
    || fail "TRUTHGATE_KUBO_ENABLE_GC must be true or false."
printf '%s\n' "${enable_gc}" >"${TMPDIR}/kubo-enable-gc"

announce_port="${TRUTHGATE_KUBO_ANNOUNCE_PORT:-}"
[[ -n "${announce_port}" ]] || announce_port="$(json_setting '.publicAnnounce.port' '')"
[[ -n "${announce_port}" && "${announce_port}" != "null" ]] || announce_port="${IPFS_SWARM_PORT:-4001}"
[[ "${announce_port}" =~ ^[0-9]+$ ]] && (( announce_port >= 1 && announce_port <= 65535 )) \
    || fail "TRUTHGATE_KUBO_ANNOUNCE_PORT must be an integer from 1 through 65535."

public_ipv4="$(resolve_public_address 4 "$(setting TRUTHGATE_KUBO_PUBLIC_IPV4 '.publicAnnounce.ipv4' auto)")"
public_ipv6="$(resolve_public_address 6 "$(setting TRUTHGATE_KUBO_PUBLIC_IPV6 '.publicAnnounce.ipv6' auto)")"

managed_addresses=()
if [[ -n "${public_ipv4}" ]]; then
    managed_addresses+=(
        "/ip4/${public_ipv4}/tcp/${announce_port}"
        "/ip4/${public_ipv4}/udp/${announce_port}/quic-v1"
        "/ip4/${public_ipv4}/udp/${announce_port}/quic-v1/webtransport"
    )
fi
if [[ -n "${public_ipv6}" ]]; then
    managed_addresses+=(
        "/ip6/${public_ipv6}/tcp/${announce_port}"
        "/ip6/${public_ipv6}/udp/${announce_port}/quic-v1"
        "/ip6/${public_ipv6}/udp/${announce_port}/quic-v1/webtransport"
    )
fi

managed_state="${TRUTHGATE_STATE_PATH}/kubo-managed-append-announce.json"
current="$(as_truthgate ipfs config Addresses.AppendAnnounce --json 2>/dev/null || printf '[]')"
previous='[]'
if [[ -s "${managed_state}" ]]; then
    previous="$(cat "${managed_state}")"
    jq -e 'type == "array"' <<<"${previous}" >/dev/null || previous='[]'
fi
without_previous="$(jq -c --argjson previous "${previous}" \
    '. as $current | [$current[] | select(. as $item | ($previous | index($item)) == null)]' \
    <<<"${current}")"
if (( ${#managed_addresses[@]} > 0 )); then
    desired="$(printf '%s\n' "${managed_addresses[@]}" | jq -R . | jq -sc .)"
else
    desired='[]'
fi
updated="$(jq -c --argjson desired "${desired}" \
    'reduce $desired[] as $item (. ; if index($item) then . else . + [$item] end)' \
    <<<"${without_previous}")"
set_json Addresses.AppendAnnounce "${updated}"
printf '%s\n' "${desired}" >"${managed_state}"
chown "${truthgate_user}:${truthgate_group}" "${managed_state}"
chmod 0640 "${managed_state}"

while IFS= read -r entry; do
    key="$(jq -r '.key' <<<"${entry}")"
    value="$(jq -c '.value' <<<"${entry}")"
    case "${key}" in
        Addresses.API|Addresses.Gateway)
            warn "Ignoring protected Kubo override '${key}'; TruthGate requires loopback-only listeners."
            ;;
        *)
            log "Applying advanced Kubo override: ${key}"
            set_json "${key}" "${value}"
            ;;
    esac
done < <(jq -c 'to_entries[]' "${TRUTHGATE_KUBO_OVERRIDES_PATH}")

set_string Addresses.API /ip4/127.0.0.1/tcp/5001
set_string Addresses.Gateway /ip4/127.0.0.1/tcp/9010

log "Kubo server configuration:"
log "  Routing.Type: $(as_truthgate ipfs config Routing.Type)"
log "  Swarm listeners: $(as_truthgate ipfs config Addresses.Swarm --json | jq -c .)"
log "  Managed public announce IPv4: ${public_ipv4:-disabled or unavailable}"
log "  Managed public announce IPv6: ${public_ipv6:-disabled or unavailable}"
log "  Public announce port: ${announce_port}"
log "  Provide.Enabled: $(as_truthgate ipfs config Provide.Enabled)"
log "  Provide.Strategy: $(as_truthgate ipfs config Provide.Strategy)"
log "  Provide.DHT.Interval: $(as_truthgate ipfs config Provide.DHT.Interval)"
log "  Provide.DHT.SweepEnabled: $(as_truthgate ipfs config Provide.DHT.SweepEnabled)"
log "  Provide.DHT.ResumeEnabled: $(as_truthgate ipfs config Provide.DHT.ResumeEnabled)"
log "  Datastore.StorageMax: $(as_truthgate ipfs config Datastore.StorageMax) (${storage_source})"
log "  Datastore.StorageGCWatermark: $(as_truthgate ipfs config Datastore.StorageGCWatermark)%"
log "  Automatic repository GC: ${enable_gc}"
