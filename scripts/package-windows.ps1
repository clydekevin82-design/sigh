param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$ReleaseVersion = "dev"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "MusicImporterApp/LtdMusicImporter.csproj"
$artifactRoot = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifactRoot "LtdMusicImporter-$Runtime"
$zipPath = Join-Path $artifactRoot "LtdMusicImporter-$ReleaseVersion-$Runtime.zip"

New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

Copy-Item (Join-Path $repoRoot "README.md") $publishDir
Copy-Item (Join-Path $repoRoot "LICENSE") $publishDir
New-Item -ItemType Directory -Force -Path (Join-Path $publishDir "docs") | Out-Null
Copy-Item (Join-Path $repoRoot "docs/EMULATOR_INSTALL.md") (Join-Path $publishDir "docs")
Copy-Item (Join-Path $repoRoot "docs/CONVERTERS.md") (Join-Path $publishDir "docs")
Get-ChildItem $publishDir -Recurse -Filter *.pdb | Remove-Item -Force

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
Write-Host "Wrote $zipPath"
