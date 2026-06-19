#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot "src\SimpleAudioRouter\SimpleAudioRouter.csproj"
$publishDir = Join-Path $repoRoot "dist\publish\win-x64"
$issFile = Join-Path $PSScriptRoot "SimpleAudioRouter.iss"

[xml]$projectXml = Get-Content $project
$version = ($projectXml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Could not read Version from $project"
}

$iconPath = Join-Path $repoRoot "src\SimpleAudioRouter\obj\Release\net10.0-windows\app.ico"
if (-not (Test-Path $iconPath)) {
    Write-Host "Generating build assets..."
    & (Join-Path $repoRoot "scripts\generate-build-assets.ps1") -Configuration Release
}

Write-Host "Publishing SimpleAudioRouter v$version (Release, win-x64, self-contained)..."

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishReadyToRun=true `
    -o $publishDir

if (-not $?) {
    throw "dotnet publish failed."
}

if (-not (Test-Path $publishDir)) {
    throw "Publish folder was not created: $publishDir"
}

$publishedExe = Join-Path $publishDir "SimpleAudioRouter.exe"
if (-not (Test-Path $publishedExe)) {
    throw "Published exe not found: $publishedExe"
}

$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LocalAppData\Programs\Inno Setup 6\ISCC.exe"
)

$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    Write-Host ""
    Write-Host "Inno Setup 6 not found. Install it, then re-run this script:"
    Write-Host "  winget install --id JRSoftware.InnoSetup -e"
    Write-Host ""
    Write-Host "Published files are ready at:"
    Write-Host "  $publishDir"
    exit 1
}

Write-Host "Building installer with Inno Setup..."
if (-not (Test-Path $iconPath)) {
    throw "Generated app.ico not found at $iconPath"
}

& $iscc `
    "/DMyAppVersion=$version" `
    "/DAppIcon=$iconPath" `
    "/DPublishDir=$publishDir" `
    $issFile

if (-not $?) {
    throw "Inno Setup compile failed."
}

$setupExe = Get-ChildItem (Join-Path $repoRoot "dist\SimpleAudioRouter-Setup-*.exe") |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

Write-Host ""
Write-Host "Done."
Write-Host "  Version:   $version"
Write-Host "  Installer: $($setupExe.FullName)"
Write-Host "  Size:      $([math]::Round($setupExe.Length / 1MB, 1)) MB"
