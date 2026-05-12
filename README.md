# LTD Custom Music Importer

Cross-platform Avalonia app for creating custom music RomFS overlays for LTD.

This repo intentionally does **not** include game files. You must provide your
own legally dumped `RomFS` locally when building or testing music imports.

## Features

- Scans `RomFS/Sound/Resource/Stream`.
- Groups slots into Music Disks, Background Music, Games, Dreams, and SFX.
- Copies converted `.bwav` files directly.
- Accepts `.wav` sources through a BWAV converter command.
- Optionally normalizes `.flac`, `.ogg`, `.mp3`, and `.m4a` through `ffmpeg`.
- Packages output as LayeredFS-style `romfs` overlays for emulators and
  Atmosphere-style mod folders.
- Experimental new stream slot mode for creating named custom `.bwav` files in
  the overlay. Stock music menus may still need table/UI hooks before those new
  files appear as selectable songs.

## Build Native Releases

Install .NET 8 SDK, then run:

```bash
RELEASE_VERSION=v0.1.0 RUNTIME=win-x64 scripts/package-release.sh
RELEASE_VERSION=v0.1.0 RUNTIME=linux-x64 scripts/package-release.sh
RELEASE_VERSION=v0.1.0 RUNTIME=osx-x64 scripts/package-release.sh
RELEASE_VERSION=v0.1.0 RUNTIME=osx-arm64 scripts/package-release.sh
```

The zips will be created under `artifacts/`.

On Windows PowerShell:

```powershell
pwsh scripts/package-windows.ps1 -Runtime win-x64 -ReleaseVersion v0.1.0
```

Manual publish equivalent:

```bash
dotnet publish MusicImporterApp/LtdMusicImporter.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/LtdMusicImporter-linux-x64
```

## Use The App

1. Open `LtdMusicImporter.exe`.
2. Select your extracted `RomFS` folder.
3. Select a source audio file.
4. Choose replace mode, or experimental new stream slot mode.
5. Choose a category and replacement slot, or enter a new slot name.
6. Import.
7. Copy the generated music overlay folder into your emulator or console mod
   directory.

The game uses Nintendo-style `.bwav` streams, not ordinary Broadcast WAV files.
For `.wav` imports, set the converter command to a tool that can write the
game's BWAV variant. The app defaults to:

```text
VGAudioCli -i {input} -o {output}
```

Replacement mode is the verified path. New stream slot mode creates the audio
file under a new stream name, but it is experimental because the game's stock
selection menus may not list new songs until separate goods/BGM/UI table patches
reference them.

## Emulator Compatibility

The generated mods are plain RomFS overlays, so they should work anywhere the
emulator supports layered filesystem replacement.

See [docs/EMULATOR_INSTALL.md](docs/EMULATOR_INSTALL.md) for Ryujinx,
Sudachi/Suyu/Yuzu-family, and console Atmosphere instructions.

## GameBanana Upload

See [gamebanana/README.md](gamebanana/README.md) for the upload checklist,
description text, credits, and release file naming.

## Release

GitHub Actions builds native release zips for every tag that starts with `v`:

- Windows x64: `win-x64`
- Linux x64: `linux-x64`
- macOS Intel: `osx-x64`
- macOS Apple Silicon: `osx-arm64`

Release example:

```bash
git tag v0.1.0
git push origin v0.1.0
```

The workflow also keeps a build artifact on normal pushes.
