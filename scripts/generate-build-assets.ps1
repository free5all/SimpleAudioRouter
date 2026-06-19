#Requires -Version 5.1
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$projectDir = Join-Path $repoRoot "src\SimpleAudioRouter"
$intermediate = Join-Path $projectDir "obj\$Configuration\net10.0-windows"

New-Item -ItemType Directory -Force -Path $intermediate | Out-Null

$iconScript = Join-Path $projectDir "assets\generate-icon.ps1"
$repoScript = Join-Path $projectDir "assets\generate-repository-info.ps1"
$iconOut = Join-Path $intermediate "app.ico"
$repoOut = Join-Path $intermediate "RepositoryInfo.g.cs"

& $iconScript -OutputPath $iconOut
& $repoScript -OutputPath $repoOut -RepoRoot $repoRoot

Write-Host "Generated build assets in $intermediate"
