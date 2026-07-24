#!/usr/bin/env bash
set -Eeuo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
compose_file="${repository_root}/compose.yaml"
dev_compose_file="${repository_root}/compose.dev.yaml"
output_file="${repository_root}/compose.rider.yaml"
temp_file="$(mktemp "${repository_root}/.compose.rider.XXXXXX.tmp")"

cleanup() {
  rm -f "${temp_file}"
}
trap cleanup EXIT

cd "${repository_root}"

docker compose \
  -f "${compose_file}" \
  -f "${dev_compose_file}" \
  config > "${temp_file}"

if [[ ! -s "${temp_file}" ]]; then
  echo "Generated Compose file is empty." >&2
  exit 1
fi

mv -f "${temp_file}" "${output_file}"
trap - EXIT

echo "Generated ${output_file}"
