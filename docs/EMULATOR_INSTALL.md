# Emulator And Console Install

The music importer creates a mod folder shaped like this:

```text
MyCustomMusic/
  romfs/
    Sound/
      Resource/
        Stream/
          BGM_Field03_Basic.bwav
  music-import.json
  README.md
```

Only the `romfs` folder is required by emulators. Keep `music-import.json` for
your own notes.

## Ryujinx

1. Right-click the game in Ryujinx.
2. Choose **Open Mods Directory**.
3. Create or copy your generated mod folder there.
4. The result should look like:

```text
Ryujinx mods directory/
  MyCustomMusic/
    romfs/
      Sound/
        Resource/
          Stream/
            <replacement>.bwav
```

5. Restart the game.

If several mods replace the same `.bwav`, the last loaded one wins. Keep one
custom-music mod active per slot unless you intentionally want an override.

## Suyu / Sudachi / Yuzu-Family Emulators

1. Right-click the game.
2. Choose **Open Mod Data Location** or **Open Mods Directory**.
3. Copy the generated mod folder into that directory.
4. Confirm this shape:

```text
load/<title id>/MyCustomMusic/
  romfs/
    Sound/
      Resource/
        Stream/
          <replacement>.bwav
```

5. Enable the mod in the game's properties if your emulator exposes a mod list.
6. Restart the game.

## Atmosphere On Console

Use your legally owned console and dumped title only.

Copy the generated `romfs` overlay to:

```text
sdmc:/atmosphere/contents/<title id>/romfs/
```

For example:

```text
sdmc:/atmosphere/contents/<title id>/romfs/Sound/Resource/Stream/<replacement>.bwav
```

Restart the game after copying files.

## Compatibility Notes

- The tool creates standard RomFS path replacements. It does not patch `ExeFS`.
- Ryujinx, Suyu, Sudachi, Yuzu-family builds, and Atmosphere all use this same
  overlay concept, but the folder you paste into differs.
- Do not include `RomFS`, `ExeFS`, `.xci`, `.nsp`, game updates, keys, or prod
  files in public uploads.
- If audio does not play, test with a short replacement first and verify your
  converter produced a Nintendo-style `.bwav`, not a regular `.wav` renamed to
  `.bwav`.
