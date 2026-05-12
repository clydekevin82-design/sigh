#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "usage: scripts/check-release-safety.sh <release.zip>" >&2
  exit 2
fi

ZIP_PATH="$1"
if [[ ! -f "$ZIP_PATH" ]]; then
  echo "missing file: $ZIP_PATH" >&2
  exit 2
fi

blocked='(^|/)(RomFS|ExeFS)(/|$)|\.(xci|nsp|nca|tik|keys)$|prod\.keys|title\.keys'
if unzip -Z1 "$ZIP_PATH" | grep -Eiq "$blocked"; then
  echo "release safety check failed: archive contains blocked game-file paths" >&2
  unzip -Z1 "$ZIP_PATH" | grep -Ei "$blocked" >&2
  exit 1
fi

echo "release safety check passed: $ZIP_PATH"
