#!/usr/bin/env bash
set -Eeuo pipefail

: "${IPFS_PATH:=/data/ipfs/repo}"
api_address="/ip4/127.0.0.1/tcp/5001"

config_value() {
    ipfs config "$1" 2>/dev/null || printf '<unset>'
}

config_json() {
    ipfs config --json "$1" 2>/dev/null | jq -c . || printf '<unset>'
}

printf 'TruthGate Kubo status\n'
printf '=====================\n'
printf 'Repository: %s\n' "${IPFS_PATH}"
printf 'Kubo version: %s\n' "$(ipfs version --number 2>/dev/null || printf '<unavailable>')"
printf 'Routing.Type: %s\n' "$(config_value Routing.Type)"
printf 'Addresses.API: %s\n' "$(config_value Addresses.API)"
printf 'Addresses.Gateway: %s\n' "$(config_value Addresses.Gateway)"
printf 'Addresses.Swarm: %s\n' "$(config_json Addresses.Swarm)"
printf 'Addresses.AppendAnnounce: %s\n' "$(config_json Addresses.AppendAnnounce)"
printf 'Swarm.EnableHolePunching: %s\n' "$(config_value Swarm.EnableHolePunching)"
printf 'Swarm.RelayClient.Enabled: %s\n' "$(config_value Swarm.RelayClient.Enabled)"
printf 'Provide.Enabled: %s\n' "$(config_value Provide.Enabled)"
printf 'Provide.Strategy: %s\n' "$(config_value Provide.Strategy)"
printf 'Provide.DHT.Interval: %s\n' "$(config_value Provide.DHT.Interval)"
printf 'Provide.DHT.SweepEnabled: %s\n' "$(config_value Provide.DHT.SweepEnabled)"
printf 'Provide.DHT.ResumeEnabled: %s\n' "$(config_value Provide.DHT.ResumeEnabled)"
printf 'Datastore.StorageMax: %s\n' "$(config_value Datastore.StorageMax)"
printf 'Datastore.StorageGCWatermark: %s%%\n' "$(config_value Datastore.StorageGCWatermark)"

if ! ipfs --api="${api_address}" id >/dev/null 2>&1; then
    printf '\nDaemon: unavailable at %s\n' "${api_address}"
    exit 1
fi

peer_id="$(ipfs --api="${api_address}" id -f='<id>')"
peer_count="$(ipfs --api="${api_address}" swarm peers 2>/dev/null | awk 'NF { count++ } END { print count + 0 }')"

printf '\nDaemon: ready\n'
printf 'Peer ID: %s\n' "${peer_id}"
printf 'Connected swarm peers: %s\n' "${peer_count}"
printf 'Active listen addresses:\n'
ipfs --api="${api_address}" swarm addrs listen 2>/dev/null | sed 's/^/  /' || true

printf '\nRepository statistics:\n'
ipfs --api="${api_address}" repo stat --human 2>/dev/null | sed 's/^/  /' || true

printf '\nAutoNAT observations:\n'
ipfs --api="${api_address}" swarm addrs autonat 2>/dev/null | sed 's/^/  /' || \
    printf '  unavailable on this Kubo build\n'

printf '\nProvider statistics:\n'
ipfs --api="${api_address}" provide stat 2>/dev/null | sed 's/^/  /' || \
    printf '  unavailable or provider is still initializing\n'
