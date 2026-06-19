# Generates a full-color multi-size .ico from assets/SimpleAudioRouter.png
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"
$assetsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pngPath = Join-Path $assetsDir "SimpleAudioRouter.png"

if (-not (Test-Path $pngPath)) {
    Write-Host "SimpleAudioRouter.png not found - skipping icon generation."
    exit 0
}

Add-Type -AssemblyName System.Drawing

function New-ScaledBitmap {
    param(
        [System.Drawing.Bitmap]$Source,
        [int]$Size
    )

    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.Clear([System.Drawing.Color]::Transparent)

        $scale = [Math]::Min($Size / $Source.Width, $Size / $Source.Height)
        $drawW = [int][Math]::Round($Source.Width * $scale)
        $drawH = [int][Math]::Round($Source.Height * $scale)
        $x = [int](($Size - $drawW) / 2)
        $y = [int](($Size - $drawH) / 2)
        $graphics.DrawImage($Source, $x, $y, $drawW, $drawH)
    }
    finally {
        $graphics.Dispose()
    }

    return $bitmap
}

function Get-PngBytes {
    param([System.Drawing.Bitmap]$Bitmap)

    $stream = New-Object System.IO.MemoryStream
    try {
        $Bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return $stream.ToArray()
    }
    finally {
        $stream.Dispose()
    }
}

function Write-IconFile {
    param(
        [string]$Path,
        [System.Collections.Generic.List[object]]$Images
    )

    $directory = Split-Path $Path -Parent
    if ($directory) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    $count = $Images.Count
    $headerSize = 6 + ($count * 16)
    $offset = $headerSize

    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    $writer = New-Object System.IO.BinaryWriter $stream
    try {
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]$count)

        foreach ($image in $Images) {
            $size = [int]$image.Size
            $pngBytes = [byte[]]$image.PngBytes
            $widthByte = if ($size -ge 256) { [byte]0 } else { [byte]$size }
            $heightByte = $widthByte

            $writer.Write($widthByte)
            $writer.Write($heightByte)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            $writer.Write([UInt16]1)
            $writer.Write([UInt16]32)
            $writer.Write([UInt32]$pngBytes.Length)
            $writer.Write([UInt32]$offset)
            $offset += $pngBytes.Length
        }

        foreach ($image in $Images) {
            $writer.Write([byte[]]$image.PngBytes)
        }
    }
    finally {
        $writer.Dispose()
    }
}

$source = [System.Drawing.Bitmap]::FromFile($pngPath)
try {
    $sizes = @(16, 24, 32, 48, 64, 128, 256)
    $images = New-Object 'System.Collections.Generic.List[object]'

    foreach ($size in $sizes) {
        $scaled = New-ScaledBitmap -Source $source -Size $size
        try {
            $pngBytes = Get-PngBytes -Bitmap $scaled
            $images.Add([PSCustomObject]@{
                Size     = $size
                PngBytes = $pngBytes
            }) | Out-Null
        }
        finally {
            $scaled.Dispose()
        }
    }

    Write-IconFile -Path $OutputPath -Images $images
    Write-Host "Wrote $OutputPath ($($images.Count) sizes, 32-bit PNG)"
}
finally {
    $source.Dispose()
}
