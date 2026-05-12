# LTD Mod Tools

Cross-platform tools and notes for making LayeredFS-style mods for LTD.

This repo intentionally does **not** include game files. You must provide your
own legally dumped `RomFS`/`ExeFS` locally when building or testing mods.

## Included Tools

- `MusicImporterApp`: Avalonia desktop app for importing custom music.
  - Scans `RomFS/Sound/Resource/Stream`.
  - Groups slots into Music Disks, Background Music, Games, Dreams, and SFX.
  - Copies converted `.bwav` files directly.
  - Accepts `.wav` sources through a BWAV converter command, with optional
    `ffmpeg` normalization for `.flac`, `.ogg`, `.mp3`, and `.m4a`.
- `tools/ltd_mod_builder.py`: prototype data-mod builder for social/drama
  tuning overlays.
- `MODDING_NOTES.md`: feature feasibility notes for further reverse engineering.

## Build The Windows App

Install .NET 8 SDK, then run:

```powershell
pwsh scripts/package-windows.ps1
```

The zip will be created under `artifacts/`.

Manual equivalent:

```bash
dotnet publish MusicImporterApp/LtdMusicImporter.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/LtdMusicImporter-win-x64
```

## Use The Music Importer

1. Open `LtdMusicImporter.exe`.
2. Select your extracted `RomFS` folder.
3. Select a source audio file.
4. Choose a category and replacement slot.
5. Import.
6. Copy the generated mod folder into your emulator or console mod directory.

The game uses Nintendo-style `.bwav` streams, not ordinary Broadcast WAV files.
For `.wav` imports, set the converter command to a tool that can write the
game's BWAV variant. The app defaults to:

```text
VGAudioCli -i {input} -o {output}
```

## Emulator Compatibility

The generated mods are plain RomFS overlays, so they should work anywhere the
emulator supports layered filesystem replacement.

See [docs/EMULATOR_INSTALL.md](docs/EMULATOR_INSTALL.md) for Ryujinx,
Sudachi/Suyu/Yuzu-family, and console Atmosphere instructions.

## GameBanana Upload

See [gamebanana/README.md](gamebanana/README.md) for the upload checklist,
description text, credits, and release file naming.

## Release

GitHub Actions builds a native Windows x64 zip for every tag that starts with
`v`, for example:

```bash
git tag v0.1.0
git push origin v0.1.0
```

The workflow also keeps a build artifact on normal pushes.
