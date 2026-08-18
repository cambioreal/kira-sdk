#!/usr/bin/env bash
set -euo pipefail

artifact_dir=${1:?"usage: verify-package-set.sh <artifact-dir> [expected-version]"}
expected_version=${2:-}

mapfile -t packages < <(find "$artifact_dir" -maxdepth 1 -type f -name 'CambioReal.*.nupkg' | sort)
if [[ ${#packages[@]} -ne 2 ]]; then
  echo "expected exactly two CambioReal packages, found ${#packages[@]}" >&2
  exit 1
fi

contracts_package=$(find "$artifact_dir" -maxdepth 1 -type f -name 'CambioReal.Contracts.*.nupkg' -print -quit)
client_package=$(find "$artifact_dir" -maxdepth 1 -type f -name 'CambioReal.Kira.Client.*.nupkg' -print -quit)
if [[ -z "$contracts_package" || -z "$client_package" ]]; then
  echo "expected CambioReal.Contracts and CambioReal.Kira.Client packages" >&2
  exit 1
fi

read_nuspec() {
  local package=$1
  local nuspec
  nuspec=$(unzip -Z1 "$package" | awk '/\.nuspec$/{print; exit}')
  unzip -p "$package" "$nuspec" | tr -d '\r\n'
}

contracts_nuspec=$(read_nuspec "$contracts_package")
client_nuspec=$(read_nuspec "$client_package")
contracts_version=$(sed -E 's/.*<version>([^<]+)<\/version>.*/\1/' <<<"$contracts_nuspec")
client_version=$(sed -E 's/.*<version>([^<]+)<\/version>.*/\1/' <<<"$client_nuspec")
contracts_floor=$(sed -E 's/.*<dependency id="CambioReal.Contracts" version="([^"]+)".*/\1/' <<<"$client_nuspec")

if [[ "$client_version" != "$contracts_version" ]]; then
  echo "package versions diverge: client=$client_version contracts=$contracts_version" >&2
  exit 1
fi

if [[ "$contracts_floor" != "$contracts_version" ]]; then
  echo "Contracts dependency floor diverges: floor=$contracts_floor package=$contracts_version" >&2
  exit 1
fi

if [[ -n "$expected_version" && "$client_version" != "$expected_version" ]]; then
  echo "package version $client_version does not match expected $expected_version" >&2
  exit 1
fi

echo "verified Kira package set $client_version (Client + Contracts floor)"
