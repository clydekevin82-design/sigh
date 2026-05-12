#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${CONFIGURATION:-Release}"
RUNTIME="${RUNTIME:-win-x64}"
RELEASE_VERSION="${RELEASE_VERSION:-dev}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$REPO_ROOT/MusicImporterApp/LtdMusicImporter.csproj"
ARTIFACT_ROOT="$REPO_ROOT/artifacts"
PUBLISH_DIR="$ARTIFACT_ROOT/LtdMusicImporter-$RUNTIME"
ZIP_PATH="$ARTIFACT_ROOT/LtdMusicImporter-$RELEASE_VERSION-$RUNTIME.zip"

mkdir -p "$ARTIFACT_ROOT"
rm -rf "$PUBLISH_DIR" "$ZIP_PATH"

dotnet publish "$PROJECT" \
  -c "$CONFIGURATION" \
  -r "$RUNTIME" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -o "$PUBLISH_DIR"

cp "$REPO_ROOT/README.md" "$PUBLISH_DIR/"
cp "$REPO_ROOT/LICENSE" "$PUBLISH_DIR/"
mkdir -p "$PUBLISH_DIR/docs"
cp "$REPO_ROOT/docs/EMULATOR_INSTALL.md" "$PUBLISH_DIR/docs/"
cp "$REPO_ROOT/docs/CONVERTERS.md" "$PUBLISH_DIR/docs/"
find "$PUBLISH_DIR" -name '*.pdb' -type f -delete

(cd "$PUBLISH_DIR" && zip -r "$ZIP_PATH" .)
echo "Wrote $ZIP_PATH"
