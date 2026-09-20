[CmdletBinding()]
param(
    [switch]$Clean,
    [switch]$PortableOnly,
    [switch]$InstallInno,
    [switch]$ForceCloseApp
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

function Ensure-AppNotRunning {
    $running = @(Get-Process -Name "Windows11Optimizer" -ErrorAction SilentlyContinue)
    if ($running.Count -eq 0) {
        return
    }

    if ($ForceCloseApp) {
        Write-Step "Cerrando Windows11Optimizer"
        foreach ($process in $running) {
            try {
                Write-Host "Cerrando PID $($process.Id)..." -ForegroundColor Yellow
                Stop-Process -Id $process.Id -Force -ErrorAction Stop
                Wait-Process -Id $process.Id -Timeout 5 -ErrorAction SilentlyContinue
            }
            catch {
                throw "No se pudo cerrar Windows11Optimizer (PID $($process.Id)). Cierra la app manualmente y vuelve a ejecutar."
            }
        }

        Start-Sleep -Milliseconds 500
        return
    }

    $pids = ($running | Select-Object -ExpandProperty Id) -join ", "
    throw "Windows11Optimizer.exe está en ejecución (PID: $pids) y bloquea artifacts\portable\Windows11Optimizer.exe. Cierra la app y vuelve a ejecutar, o usa: .\exe-generated.ps1 -Clean -PortableOnly -ForceCloseApp"
}

function Remove-ArtifactsSafely {
    if (-not (Test-Path $Artifacts)) {
        return
    }

    Ensure-AppNotRunning
    Write-Step "Limpiando artifacts"

    $lastError = $null
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            Remove-Item $Artifacts -Recurse -Force -ErrorAction Stop
            return
        }
        catch {
            $lastError = $_
            if ($attempt -lt 3) {
                Write-Host "Archivo bloqueado. Reintentando limpieza ($attempt/3)..." -ForegroundColor Yellow
                Start-Sleep -Seconds 1
            }
        }
    }

    throw "No se pudo limpiar '$Artifacts'. Cierra Windows11Optimizer.exe, Explorador de archivos que esté previsualizando el EXE, o cualquier antivirus que lo esté inspeccionando. Detalle: $($lastError.Exception.Message)"
}

if ($env:OS -ne "Windows_NT") {
    throw "Este generador está diseñado para Windows."
}

if (-not (Test-Path $Project)) {
    throw "No se encontró el proyecto: $Project"
}

if ($Clean) {
    Remove-ArtifactsSafely
}

New-Item $PublishDir -ItemType Directory -Force | Out-Null
New-Item $PortableDir -ItemType Directory -Force | Out-Null
New-Item $InstallerDir -ItemType Directory -Force | Out-Null

$dotnet = Get-Command "dotnet.exe" -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "No se encontró .NET SDK. Instala .NET 8 o superior y vuelve a ejecutar este archivo."
}

Write-Step "Comprobando .NET SDK"
$sdkLines = & dotnet --list-sdks
if ($LASTEXITCODE -ne 0 -or -not $sdkLines) {
    throw "No fue posible consultar los SDK de .NET instalados."
}

$detectedMajors = foreach ($line in $sdkLines) {
    if ($line -match "^(\d+)\.") {
        [int]$Matches[1]
    }
}

$highestMajor = ($detectedMajors | Measure-Object -Maximum).Maximum
if (-not $highestMajor -or $highestMajor -lt 8) {
    throw "Se requiere .NET SDK 8 o superior. SDK detectados: $($sdkLines -join '; ')"
}

Write-Host ($sdkLines -join [Environment]::NewLine)
Write-Host "SDK compatible detectado: .NET $highestMajor.x" -ForegroundColor Green
Write-Host "El proyecto seguirá compilándose para net8.0-windows." -ForegroundColor DarkGray

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

Ensure-AppNotRunning

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
