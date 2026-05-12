# Audio Converter Notes

The importer packages music overlays, but it does not ship proprietary codecs
or game files. The target files are Nintendo-style `.bwav` streams.

## Recommended Flow

1. Start with a clean `.wav`.
2. Convert it with a tool that supports Nintendo BWAV/BFSTP-style streams.
3. Use the importer to place the converted file into the selected game slot.

The app default converter command is:

```text
VGAudioCli -i {input} -o {output}
```

If your converter uses different arguments, replace the template. The app
substitutes:

- `{input}` with the source WAV path.
- `{output}` with the selected slot path in the generated mod overlay.

## Non-WAV Sources

When importing `.flac`, `.ogg`, `.mp3`, or `.m4a`, the app first runs:

```text
ffmpeg -y -i {input} -acodec pcm_s16le {temp wav}
```

Then it passes the temporary WAV to your BWAV converter.

## Slot Matching

For best results, replace a slot with audio close to the original slot's role:

- Music Disks: short record-player songs.
- Background Music: longer looping BGM.
- Games: short minigame/video-game cues.
- Dreams: dream sequence music.
- SFX: short one-shot sounds.

Long files in short SFX slots may work but can be cut off by game logic.

## New Stream Slot Mode

New stream slot mode creates a new `.bwav` file name instead of replacing an
existing one. This is mainly for advanced modders. The raw file will exist in
the overlay, but the game needs some piece of data or code to reference that
new stream name before it becomes selectable in stock UI.
