#!/usr/bin/env bash
set -Eeuo pipefail

ipfs --api=/ip4/127.0.0.1/tcp/5001 diag healthy >/dev/null

health_url="${TRUTHGATE_HEALTH_URL:-https://127.0.0.1:443/}"
http_status="$(curl \
    --silent \
    --show-error \
    --insecure \
    --max-time 5 \
    --output /dev/null \
    --write-out '%{http_code}' \
    "${health_url}")"

[[ "${http_status}" =~ ^[0-9]{3}$ ]]
(( 10#${http_status} >= 200 && 10#${http_status} < 500 ))
