<#
.SYNOPSIS
    Builds both installer variants for File Mover Service.

.PARAMETER Version
    Semantic version string written into the installers (e.g. 1.2.3).

.PARAMETER SkipSmall
    Skip building the small (framework-dependent) installer.

.PARAMETER SkipBundled
    Skip building the bundled (self-contained, includes .NET) installer.

.EXAMPLE
    .\build-release.ps1 -Version 1.0.0
    Builds both installers into .\Output\
        FileMoverServiceInstaller.exe       (~3 MB, requires .NET 10)
        FileMoverServiceInstaller-Full.exe  (~80 MB, .NET included)
#>
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [switch]$SkipSmall,
    [switch]$SkipBundled
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Step([string]$msg) {
    Write-Host ""
    Write-Host ">>> $msg" -ForegroundColor Cyan
}

function Require([string]$tool, [string]$hint) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        Write-Host "ERROR: '$tool' not found.  $hint" -ForegroundColor Red
        exit 1
    }
}

function Invoke-Dotnet([string]$cmd) {
    cmd /c "dotnet $cmd"
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($cmd.Split(' ')[0]) failed (exit $LASTEXITCODE)" }
}

# ---------------------------------------------------------------------------
# Prerequisites
# ---------------------------------------------------------------------------

Require "dotnet" "Install the .NET SDK from https://dot.net"

$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    Write-Host "ERROR: Inno Setup 6 not found at '$iscc'." -ForegroundColor Red
    Write-Host "       Download from https://jrsoftware.org/isdl.php"
    exit 1
}

# ---------------------------------------------------------------------------
# Clean
# ---------------------------------------------------------------------------

Step "Cleaning previous build artefacts"

foreach ($folder in @("publish-service", "publish-ui", "publish-service-bundled", "publish-ui-bundled")) {
    if (Test-Path $folder) {
        Remove-Item $folder -Recurse -Force
        Write-Host "  Removed $folder"
    }
}

New-Item -ItemType Directory -Force -Path "Output" | Out-Null

# ---------------------------------------------------------------------------
# Publish
# ---------------------------------------------------------------------------

$serviceProj = "FileMoverService\FileMoverService.csproj"
$uiProj      = "FileMoverService.UI\FileMoverService.UI.csproj"
$rid         = "win-x64"
$config      = "Release"

if (-not $SkipSmall) {
    Step "Publishing framework-dependent (small installer)"
    Invoke-Dotnet "publish `"$serviceProj`" -c $config -r $rid --self-contained false -o publish-service"
    Invoke-Dotnet "publish `"$uiProj`"      -c $config -r $rid --self-contained false -o publish-ui"
}

if (-not $SkipBundled) {
    Step "Publishing self-contained (bundled installer)"
    Invoke-Dotnet "publish `"$serviceProj`" -c $config -r $rid --self-contained true -o publish-service-bundled"
    Invoke-Dotnet "publish `"$uiProj`"      -c $config -r $rid --self-contained true -o publish-ui-bundled"
}

# ---------------------------------------------------------------------------
# Compile installers
# ---------------------------------------------------------------------------

$isccBase = "/DAppVersion=$Version"

if (-not $SkipSmall) {
    Step "Compiling small installer  ->  FileMoverServiceInstaller.exe"
    & $iscc $isccBase "FileMoverService.iss"
    if ($LASTEXITCODE -ne 0) { Write-Host "Inno Setup failed." -ForegroundColor Red; exit 1 }
}

if (-not $SkipBundled) {
    Step "Compiling bundled installer  ->  FileMoverServiceInstaller-Full.exe"
    & $iscc $isccBase "/DBundled" "FileMoverService.iss"
    if ($LASTEXITCODE -ne 0) { Write-Host "Inno Setup failed." -ForegroundColor Red; exit 1 }
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Step "Build complete"
Write-Host ""

Get-ChildItem "Output\FileMoverServiceInstaller*.exe" -ErrorAction SilentlyContinue | ForEach-Object {
    $sizeMb = [math]::Round($_.Length / 1MB, 1)
    Write-Host ("  {0,-45} {1,6} MB" -f $_.Name, $sizeMb) -ForegroundColor Green
}

Write-Host ""
