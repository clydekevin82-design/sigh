# Contributing

Thanks for helping improve the tools.

## Do Not Commit Game Files

Never commit or attach:

- `RomFS/`
- `ExeFS/`
- `.xci`, `.nsp`, game updates, firmware, keys, or title dumps
- Converted files extracted from the game

PRs should contain source code, docs, scripts, and original test fixtures only.

## Local Testing

Build the app:

```bash
dotnet restore MusicImporterApp/LtdMusicImporter.csproj -r linux-x64
dotnet build MusicImporterApp/LtdMusicImporter.csproj -r linux-x64
```

Build the Windows release zip:

```bash
RELEASE_VERSION=localtest scripts/package-windows.sh
```

Before sharing a release zip, inspect it:

```bash
unzip -l artifacts/LtdMusicImporter-localtest-win-x64.zip
```

It should contain the app, docs, and license only.
