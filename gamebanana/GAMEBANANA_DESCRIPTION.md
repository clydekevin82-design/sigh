# LTD Custom Music Importer

A native Windows tool for creating custom music RomFS overlays for LTD.

## Features

- Import custom music into existing stream slots.
- Slot categories for:
  - Music Disks
  - Background Music
  - Games
  - Dreams
  - SFX
- Builds LayeredFS-style `romfs` overlays.
- Works with Ryujinx, Suyu/Sudachi/Yuzu-family emulators, and Atmosphere-style
  console mod folders.
- Copies already-converted `.bwav` files directly.
- Accepts `.wav` through a configurable BWAV converter command.
- Can normalize `.flac`, `.ogg`, `.mp3`, and `.m4a` through `ffmpeg` first.

## Requirements

- Windows x64.
- Your own legally dumped `RomFS`.
- A converter that can produce the game's Nintendo-style `.bwav` stream files,
  such as `VGAudioCli`.
- Optional: `ffmpeg` for non-WAV source audio.

## Basic Use

1. Launch `LtdMusicImporter.exe`.
2. Select your extracted `RomFS` folder.
3. Pick a source audio file.
4. Choose a category and replacement slot.
5. Click **Import Music**.
6. Copy the generated mod folder into your emulator or console mod directory.

## Ryujinx

Right-click the game, choose **Open Mods Directory**, then copy the generated
mod folder there.

Expected shape:

```text
MyCustomMusic/
  romfs/
    Sound/
      Resource/
        Stream/
          <replacement>.bwav
```

## Suyu / Sudachi / Yuzu-Family

Right-click the game, choose **Open Mod Data Location** or **Open Mods
Directory**, then copy the generated mod folder there.

Expected shape:

```text
load/<title id>/MyCustomMusic/
  romfs/
    Sound/
      Resource/
        Stream/
          <replacement>.bwav
```

## Notes

This upload contains only original tooling. It does not contain game files,
title keys, firmware, updates, or copyrighted assets.

The game's `.bwav` files are Nintendo-style stream files, not standard
Broadcast WAV files. Do not rename ordinary `.wav` files to `.bwav`; convert
them with a compatible audio tool first.
