# LTD Custom Music Importer

Cross-platform Avalonia desktop app for creating custom music RomFS overlays.

## Run

```bash
dotnet run --project MusicImporterApp/LtdMusicImporter.csproj -r linux-x64
```

Use the matching runtime identifier for other platforms, for example
`win-x64` or `osx-x64`.

## Workflow

1. Select the extracted `RomFS` folder.
2. Select a source audio file. `.wav` and `.bwav` are the main paths; `.flac`,
   `.ogg`, `.mp3`, and `.m4a` are normalized through `ffmpeg` first.
3. Pick a replacement category and slot:
   - Music Disks: `SE_Record_*`
   - Background Music: `BGM_*`
   - Games: minigame/video game streams
   - Dreams: dream/inside-head streams
   - SFX: everything else
4. Choose an output folder and mod name.
5. Import.

If the source file is already `.bwav`, the app copies it directly into:

```text
<output>/<mod name>/romfs/Sound/Resource/Stream/<selected slot>
```

For `.wav`, provide a BWAV converter command template with `{input}` and
`{output}` placeholders. The default is:

```text
VGAudioCli -i {input} -o {output}
```

For `.flac`, `.ogg`, `.mp3`, or `.m4a`, the app first runs `ffmpeg` to make a
temporary 16-bit PCM WAV, then sends that WAV to the BWAV converter.

The game's `.bwav` files are Nintendo-style stream files, not standard
Microsoft Broadcast WAV files. Use a converter that supports the game's BWAV
variant; the app handles slot selection and overlay packaging around that
conversion step.

The generated mod also includes `music-import.json` and a short `README.md`.
