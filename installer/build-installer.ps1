<#
  Build the Rewind installer.

  Steps:
    1. dotnet publish of the main app (self-contained, win-x64)
    2. dotnet publish of the launcher (self-contained, win-x64, single-file)
    3. Run Inno Setup (iscc) to produce build/installer/RewindSetup.exe

  Requirements:
    - .NET SDK 10.x        (https://dotnet.microsoft.com/)
    - Inno Setup 6         (https://jrsoftware.org/isinfo.php)

  Usage (from repo root):
      powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
#>

[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime       = "win-x64"
)

$ErrorActionPreference = "Stop"

# --- Paths ------------------------------------------------------------------
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Resolve-Path (Join-Path $ScriptDir "..")
$BuildDir  = Join-Path $RepoRoot "build"
$AppOut    = Join-Path $BuildDir "app"
$LaunOut   = Join-Path $BuildDir "launcher"
$InstOut   = Join-Path $BuildDir "installer"

$AppProj      = Join-Path $RepoRoot "Rewind\Rewind.csproj"
$LauncherProj = Join-Path $ScriptDir "RewindLauncher\RewindLauncher.csproj"
$IssScript    = Join-Path $ScriptDir "setup.iss"

# --- Find Inno Setup --------------------------------------------------------
function Find-Iscc {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    $cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

$Iscc = Find-Iscc
if (-not $Iscc) {
    Write-Error "Inno Setup (ISCC.exe) not found. Install Inno Setup 6 from https://jrsoftware.org/isdl.php"
}

# --- Clean ------------------------------------------------------------------
Write-Host "==> Cleaning $BuildDir" -ForegroundColor Cyan
if (Test-Path $BuildDir) { Remove-Item $BuildDir -Recurse -Force }
New-Item -ItemType Directory -Path $AppOut, $LaunOut, $InstOut | Out-Null

# --- Prefetch vendor blobs (Docker Desktop + postgres image) ----------------
# Downloaded once and cached in installer\vendor\. Re-run is fast.
$VendorDir   = Join-Path $ScriptDir "vendor"
$DockerExe   = Join-Path $VendorDir "DockerDesktopInstaller.exe"
$PgImageTar  = Join-Path $VendorDir "postgres-17-alpine.tar"
$PgImageRef  = "postgres:17-alpine"
$DockerUrl   = "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe"

if (-not (Test-Path $VendorDir)) { New-Item -ItemType Directory -Path $VendorDir | Out-Null }

if (Test-Path $DockerExe) {
    Write-Host "==> Docker Desktop installer cached: $DockerExe" -ForegroundColor DarkGray
} else {
    Write-Host "==> Downloading Docker Desktop installer (~700 MB, one time)..." -ForegroundColor Cyan
    try {
        $progressPref = $ProgressPreference
        $ProgressPreference = 'SilentlyContinue'  # speeds up Invoke-WebRequest considerably
        Invoke-WebRequest -Uri $DockerUrl -OutFile $DockerExe -UseBasicParsing
        $ProgressPreference = $progressPref
    } catch {
        throw "Failed to download Docker Desktop: $($_.Exception.Message)"
    }
}

if (Test-Path $PgImageTar) {
    Write-Host "==> Postgres image cached: $PgImageTar" -ForegroundColor DarkGray
} else {
    Write-Host "==> Pulling and exporting $PgImageRef ..." -ForegroundColor Cyan
    docker pull $PgImageRef
    if ($LASTEXITCODE -ne 0) { throw "docker pull failed. Is Docker running on the build machine?" }
    docker save -o $PgImageTar $PgImageRef
    if ($LASTEXITCODE -ne 0) { throw "docker save failed." }
}

# --- Generate icon ----------------------------------------------------------
Write-Host "==> Generating rewind.ico from rewind_logo.png" -ForegroundColor Cyan
$LogoPng    = Join-Path $RepoRoot "Rewind\Images\rewind_logo.png"
$AppIco     = Join-Path $RepoRoot "Rewind\Images\rewind.ico"
$LauncherIco= Join-Path $ScriptDir "RewindLauncher\rewind.ico"
& (Join-Path $ScriptDir "make-ico.ps1") -Png $LogoPng -Ico $AppIco
Copy-Item -Force $AppIco $LauncherIco

# --- Publish app ------------------------------------------------------------
Write-Host "==> dotnet publish Rewind ($Runtime, self-contained)" -ForegroundColor Cyan
dotnet publish $AppProj `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $AppOut
if ($LASTEXITCODE -ne 0) { throw "Failed to publish Rewind" }

# --- Publish launcher -------------------------------------------------------
Write-Host "==> dotnet publish RewindLauncher ($Runtime, single-file)" -ForegroundColor Cyan
dotnet publish $LauncherProj `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $LaunOut
if ($LASTEXITCODE -ne 0) { throw "Failed to publish RewindLauncher" }

# Single-file publish embeds content; copy the icon next to the exe so Inno Setup can use it.
Copy-Item -Force $LauncherIco (Join-Path $LaunOut "rewind.ico")

# --- Inno Setup -------------------------------------------------------------
Write-Host "==> Building installer with Inno Setup" -ForegroundColor Cyan
& $Iscc `
    "/DAppPublishDir=$AppOut" `
    "/DLauncherPublishDir=$LaunOut" `
    "/DOutputDir=$InstOut" `
    $IssScript
if ($LASTEXITCODE -ne 0) { throw "ISCC failed" }

$installer = Join-Path $InstOut "RewindSetup.exe"
if (Test-Path $installer) {
    Write-Host ""
    Write-Host "Done! Installer: $installer" -ForegroundColor Green
} else {
    Write-Error "Installer was not produced."
}
