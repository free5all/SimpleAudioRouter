#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot "src\SimpleAudioRouter\SimpleAudioRouter.csproj"
$publishDir = Join-Path $repoRoot "dist\publish\win-x64"
$issFile = Join-Path $PSScriptRoot "SimpleAudioRouter.iss"

Write-Host "Publishing SimpleAudioRouter (Release, win-x64, self-contained)..."
dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishReadyToRun=true `
    -o $publishDir

if (-not $?) {
    throw "dotnet publish failed."
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
& $iscc $issFile

if (-not $?) {
    throw "Inno Setup compile failed."
}

$setupExe = Get-ChildItem (Join-Path $repoRoot "dist\SimpleAudioRouter-Setup-*.exe") |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

Write-Host ""
Write-Host "Done."
Write-Host "  Installer: $($setupExe.FullName)"
Write-Host "  Size:      $([math]::Round($setupExe.Length / 1MB, 1)) MB"
