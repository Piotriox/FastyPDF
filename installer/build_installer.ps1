<#
.SYNOPSIS
    Builds the FastyPDF release bundle and compiles the NSIS installer.
#>
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$projectDir = Resolve-Path (Join-Path $scriptDir "..\src\FastyPDF")
$publishDir = Join-Path $scriptDir "publish"
$nsiScript = Join-Path $scriptDir "fastypdf.nsi"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Building FastyPDF Installer ($Configuration) " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Clean old publish artifacts
if (Test-Path $publishDir) {
    Write-Host "Cleaning old publish directory..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $publishDir
}

# 2. Publish Project
Write-Host "Publishing FastyPDF project..." -ForegroundColor Green
$publishArgs = @(
    "publish",
    "$projectDir\FastyPDF.csproj",
    "-c", $Configuration,
    "-p:Platform=x64",
    "-o", $publishDir
)
& dotnet $publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

# 3. Locate makensis.exe
$nsisCandidates = @(
    "makensis.exe",
    "C:\Program Files (x86)\NSIS\makensis.exe",
    "C:\Program Files\NSIS\makensis.exe"
)

$makensis = $null
foreach ($candidate in $nsisCandidates) {
    if (Get-Command $candidate -ErrorAction SilentlyContinue) {
        $makensis = $candidate
        break
    }
    if (Test-Path $candidate) {
        $makensis = $candidate
        break
    }
}

if (-not $makensis) {
    throw "makensis.exe not found! Please install NSIS or add it to PATH."
}

Write-Host "Found NSIS compiler: $makensis" -ForegroundColor Green

# 4. Compile NSIS script
Write-Host "Compiling installer with NSIS..." -ForegroundColor Green
Push-Location $scriptDir
try {
    & $makensis $nsiScript
    if ($LASTEXITCODE -ne 0) {
        throw "makensis failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Installer generated successfully!        " -ForegroundColor Green
Write-Host " Location: $(Join-Path $scriptDir 'FastyPDF_Setup_v1.0.2.exe')" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
