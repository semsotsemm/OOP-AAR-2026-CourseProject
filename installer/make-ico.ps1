<#
  Wrap a PNG into a single-entry .ico file (PNG-embedded, supported on Vista+).
  Usage:  powershell -File make-ico.ps1 -Png path\to.png -Ico path\to.ico
#>
param(
    [Parameter(Mandatory=$true)][string]$Png,
    [Parameter(Mandatory=$true)][string]$Ico
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Png)) { throw "PNG not found: $Png" }

Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile((Resolve-Path $Png))
$w = $img.Width
$h = $img.Height
$img.Dispose()

$pngBytes = [IO.File]::ReadAllBytes((Resolve-Path $Png))

# Width/height fields are 1 byte, 0 means 256. For >=256, write 0.
$wb = if ($w -ge 256) { 0 } else { [byte]$w }
$hb = if ($h -ge 256) { 0 } else { [byte]$h }

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)

# ICONDIR (6 bytes)
$bw.Write([UInt16]0)      # reserved
$bw.Write([UInt16]1)      # type = 1 (icon)
$bw.Write([UInt16]1)      # count = 1

# ICONDIRENTRY (16 bytes)
$bw.Write([byte]$wb)              # width
$bw.Write([byte]$hb)              # height
$bw.Write([byte]0)                # colorCount
$bw.Write([byte]0)                # reserved
$bw.Write([UInt16]1)              # planes
$bw.Write([UInt16]32)             # bitCount
$bw.Write([UInt32]$pngBytes.Length)
$bw.Write([UInt32]22)             # offset = 6 + 16

# Image data
$bw.Write($pngBytes)
$bw.Flush()

$dir = Split-Path -Parent $Ico
if ($dir -and -not (Test-Path $dir)) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}
[IO.File]::WriteAllBytes($Ico, $ms.ToArray())
$bw.Dispose()
$ms.Dispose()

Write-Host "Wrote $Ico  ($($w)x$($h), $([IO.File]::ReadAllBytes($Ico).Length) bytes)"
