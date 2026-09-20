[CmdletBinding()]
param(
    [switch]$Clean,
    [switch]$PortableOnly,
    [switch]$InstallInno
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $Root "src\Windows11Optimizer\Windows11Optimizer.csproj"
$Solution = Join-Path $Root "Windows11Optimizer.sln"
$Artifacts = Join-Path $Root "artifacts"
$PublishDir = Join-Path $Artifacts "publish"
$PortableDir = Join-Path $Artifacts "portable"
$InstallerDir = Join-Path $Artifacts "installer"
$IssFile = Join-Path $Root "installer\Windows11Optimizer.iss"

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Find-InnoCompiler {
    $cmd = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $pf86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    $candidates = @(
        $(if ($pf86) { Join-Path $pf86 "Inno Setup 6\ISCC.exe" }),
        $(if ($env:ProgramFiles) { Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe" }),
        $(if ($env:LOCALAPPDATA) { Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe" })
    ) | Where-Object { $_ -and (Test-Path $_) }

    return $candidates | Select-Object -First 1
}

if ($env:OS -ne "Windows_NT") {
    throw "Este generador está diseñado para Windows."
}

if (-not (Test-Path $Project)) {
    throw "No se encontró el proyecto: $Project"
}

if ($Clean -and (Test-Path $Artifacts)) {
    Write-Step "Limpiando artifacts"
    Remove-Item $Artifacts -Recurse -Force
}

New-Item $PublishDir -ItemType Directory -Force | Out-Null
New-Item $PortableDir -ItemType Directory -Force | Out-Null
New-Item $InstallerDir -ItemType Directory -Force | Out-Null

$dotnet = Get-Command "dotnet.exe" -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "No se encontró .NET SDK. Instala .NET 8 SDK y vuelve a ejecutar este archivo."
}

Write-Step "Comprobando .NET SDK"
$sdkList = (& dotnet --list-sdks) -join [Environment]::NewLine
if ($sdkList -notmatch "(?m)^8\.") {
    throw "Se requiere .NET 8 SDK. SDK detectados: $sdkList"
}
Write-Host $sdkList

Write-Step "Restaurando paquetes"
& dotnet restore $Solution
if ($LASTEXITCODE -ne 0) { throw "dotnet restore falló." }

Write-Step "Compilando Release"
& dotnet build $Solution -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build falló." }

Write-Step "Publicando Windows x64 self-contained"
& dotnet publish $Project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló." }

$exe = Join-Path $PublishDir "Windows11Optimizer.exe"
if (-not (Test-Path $exe)) {
    throw "La publicación terminó, pero no apareció Windows11Optimizer.exe."
}

Copy-Item $exe (Join-Path $PortableDir "Windows11Optimizer.exe") -Force
Copy-Item (Join-Path $Root "LICENSE") (Join-Path $PortableDir "LICENSE.txt") -Force
Copy-Item (Join-Path $Root "THIRD-PARTY-NOTICES.md") (Join-Path $PortableDir "THIRD-PARTY-NOTICES.md") -Force

Write-Host ""
Write-Host "EXE portable generado:" -ForegroundColor Green
Write-Host "  $(Join-Path $PortableDir 'Windows11Optimizer.exe')"

if ($PortableOnly) {
    Write-Host ""
    Write-Host "PortableOnly activo. No se generará instalador." -ForegroundColor Yellow
    exit 0
}

$iscc = Find-InnoCompiler

if (-not $iscc -and $InstallInno) {
    Write-Step "Instalando Inno Setup mediante winget"
    $winget = Get-Command "winget.exe" -ErrorAction SilentlyContinue
    if (-not $winget) {
        throw "No se encontró winget. Instala Inno Setup 6 manualmente y vuelve a ejecutar este script."
    }

    & winget install --id JRSoftware.InnoSetup -e --accept-source-agreements --accept-package-agreements
    if ($LASTEXITCODE -ne 0) {
        throw "winget no pudo instalar Inno Setup."
    }

    $iscc = Find-InnoCompiler
}

if (-not $iscc) {
    Write-Host ""
    Write-Host "No se encontró Inno Setup 6." -ForegroundColor Yellow
    Write-Host "El EXE portable ya está listo."
    Write-Host ""
    Write-Host "Para crear también el instalador:"
    Write-Host "  1) Instala Inno Setup 6"
    Write-Host "  2) Ejecuta: .\exe-generated.ps1"
    Write-Host ""
    Write-Host "O permite que este script lo instale con winget:"
    Write-Host "  .\exe-generated.ps1 -InstallInno"
    exit 0
}

Write-Step "Generando instalador con Inno Setup"
& $iscc $IssFile
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup no pudo generar el instalador."
}

$setup = Join-Path $InstallerDir "Windows11Optimizer-Setup-x64.exe"
if (-not (Test-Path $setup)) {
    throw "Inno Setup terminó pero no se encontró el instalador esperado."
}

Write-Host ""
Write-Host "LISTO" -ForegroundColor Green
Write-Host "Portable:"
Write-Host "  $(Join-Path $PortableDir 'Windows11Optimizer.exe')"
Write-Host "Instalador:"
Write-Host "  $setup"
