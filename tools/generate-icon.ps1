<#
.SYNOPSIS
    Generates the AI Terminal Launcher application icon (app.ico).

.DESCRIPTION
    Programmatically renders a terminal-prompt style icon (">_" on a dark
    rounded background) at multiple resolutions and packs them into a single
    multi-frame .ico file. Each frame is stored as a PNG (the modern ICO
    format supported by Windows Vista and later, and required for 256x256).

    A 256px PNG preview is also written next to this script so the result can
    be inspected visually.

.NOTES
    Run: powershell -NoProfile -ExecutionPolicy Bypass -File tools\generate-icon.ps1
#>

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
$icoPath = Join-Path $repoRoot 'src\AITerminalLauncher.App\app.ico'
$previewPath = Join-Path $PSScriptRoot 'icon-preview.png'

# Color palette (terminal-dark background, mint-green accent).
$bgTop = [System.Drawing.Color]::FromArgb(255, 35, 40, 56)    # #232838
$bgBottom = [System.Drawing.Color]::FromArgb(255, 16, 19, 27)  # #10131B
$accent = [System.Drawing.Color]::FromArgb(255, 61, 220, 151)  # #3DDC97

function New-RoundedRectPath {
    param(
        [float] $X, [float] $Y, [float] $W, [float] $H, [float] $Radius
    )

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $Radius * 2.0
    $path.AddArc($X, $Y, $d, $d, 180, 90)
    $path.AddArc($X + $W - $d, $Y, $d, $d, 270, 90)
    $path.AddArc($X + $W - $d, $Y + $H - $d, $d, $d, 0, 90)
    $path.AddArc($X, $Y + $H - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconFramePng {
    param([int] $Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.Clear([System.Drawing.Color]::Transparent)

        # Rounded dark background.
        $pad = [float]($Size * 0.04)
        $side = [float]($Size - 2 * $pad)
        $radius = [float]($Size * 0.22)
        $rect = New-Object System.Drawing.RectangleF($pad, $pad, $side, $side)
        $bgPath = New-RoundedRectPath -X $pad -Y $pad -W $side -H $side -Radius $radius
        $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $bgTop, $bgBottom, 90.0)
        try {
            $g.FillPath($bgBrush, $bgPath)
        }
        finally {
            $bgBrush.Dispose()
            $bgPath.Dispose()
        }

        # ">_" terminal prompt drawn as geometry for crispness at all sizes.
        $stroke = [float][Math]::Max(1.5, $Size * 0.085)
        $pen = New-Object System.Drawing.Pen($accent, $stroke)
        try {
            $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

            $s = [float] $Size
            $chevron = [System.Drawing.PointF[]]@(
                (New-Object System.Drawing.PointF(($s * 0.30), ($s * 0.34))),
                (New-Object System.Drawing.PointF(($s * 0.52), ($s * 0.50))),
                (New-Object System.Drawing.PointF(($s * 0.30), ($s * 0.66)))
            )
            $g.DrawLines($pen, $chevron)
            $g.DrawLine($pen, ($s * 0.40), ($s * 0.66), ($s * 0.70), ($s * 0.66))
        }
        finally {
            $pen.Dispose()
        }

        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        # Comma prevents PowerShell from unrolling the byte[] into the pipeline.
        return , $ms.ToArray()
    }
    finally {
        $g.Dispose()
        $bmp.Dispose()
    }
}

# Little-endian writers (avoids BinaryWriter overload ambiguity in PowerShell).
function Add-UInt16 {
    param([System.Collections.Generic.List[byte]] $Buffer, [int] $Value)
    $Buffer.Add([byte]($Value -band 0xFF))
    $Buffer.Add([byte](($Value -shr 8) -band 0xFF))
}

function Add-UInt32 {
    param([System.Collections.Generic.List[byte]] $Buffer, [long] $Value)
    $Buffer.Add([byte]($Value -band 0xFF))
    $Buffer.Add([byte](($Value -shr 8) -band 0xFF))
    $Buffer.Add([byte](($Value -shr 16) -band 0xFF))
    $Buffer.Add([byte](($Value -shr 24) -band 0xFF))
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = foreach ($size in $sizes) {
    [pscustomobject]@{
        Size = $size
        Bytes = [byte[]] (New-IconFramePng -Size $size)
    }
}

$buffer = New-Object System.Collections.Generic.List[byte]

# ICONDIR
Add-UInt16 $buffer 0          # reserved
Add-UInt16 $buffer 1          # type = icon
Add-UInt16 $buffer $frames.Count

# ICONDIRENTRY for each frame
$offset = 6 + 16 * $frames.Count
foreach ($frame in $frames) {
    $dimension = if ($frame.Size -ge 256) { 0 } else { $frame.Size }
    $buffer.Add([byte] $dimension)   # width  (0 => 256)
    $buffer.Add([byte] $dimension)   # height (0 => 256)
    $buffer.Add([byte] 0)            # color count
    $buffer.Add([byte] 0)            # reserved
    Add-UInt16 $buffer 1             # color planes
    Add-UInt16 $buffer 32            # bits per pixel
    Add-UInt32 $buffer $frame.Bytes.Length
    Add-UInt32 $buffer $offset
    $offset += $frame.Bytes.Length
}

# Image data
foreach ($frame in $frames) {
    $buffer.AddRange($frame.Bytes)
}

[System.IO.File]::WriteAllBytes($icoPath, $buffer.ToArray())

# 256px PNG preview for visual inspection.
$previewBytes = ($frames | Where-Object { $_.Size -eq 256 }).Bytes
[System.IO.File]::WriteAllBytes($previewPath, $previewBytes)

Write-Output "Wrote icon : $icoPath ($($buffer.Count) bytes, $($frames.Count) frames)"
Write-Output "Wrote preview: $previewPath"
