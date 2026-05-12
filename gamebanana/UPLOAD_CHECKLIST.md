# Upload Checklist

- [ ] Build `LtdMusicImporter-<version>-win-x64.zip`.
- [ ] Open the zip and confirm it contains `LtdMusicImporter.exe`.
- [ ] Confirm the zip does not contain `RomFS`, `ExeFS`, `.nsp`, `.xci`, keys,
      firmware, or dumped game files.
- [ ] Paste `GAMEBANANA_DESCRIPTION.md` into the description field.
- [ ] Use `RELEASE_NOTES.md` as the changelog.
- [ ] Add screenshots of the app, not screenshots containing copyrighted game
      assets unless the site/game page permits them.
- [ ] Mark requirements: dumped RomFS, BWAV converter, optional ffmpeg.
- [ ] Test a generated overlay in Ryujinx or another emulator before marking
      the upload stable.
