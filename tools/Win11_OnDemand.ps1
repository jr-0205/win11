#Requires -RunAsAdministrator
<#
Win11_OnDemand.ps1
Optimización reversible de servicios de terceros observados en el equipo.

MODOS:
  Optimize       -> detiene y deshabilita servicios seleccionados para que NO arranquen solos
  Start-VMware   -> habilita temporalmente e inicia VMware DHCP/NAT
  Stop-VMware    -> detiene y vuelve a deshabilitar VMware DHCP/NAT
  Start-Acer     -> habilita temporalmente e inicia utilidades Acer seleccionadas
  Stop-Acer      -> detiene y vuelve a deshabilitar esas utilidades Acer
  Restore        -> restaura los tipos de inicio y tareas guardados antes de Optimize
  Status         -> muestra el estado actual

IMPORTANTE:
- NO toca Defender, Windows Update, Search, SysMain, audio, Wi-Fi, Bluetooth,
  touchpad, servicios de entrada, AMD graphics ni Acer Quick Access.
- NO desinstala nada.
- La primera ejecución de Optimize crea una copia de seguridad.
#>

[CmdletBinding()]
param(
    [ValidateSet(
        "Optimize",
        "Start-VMware",
        "Stop-VMware",
        "Start-Acer",
        "Stop-Acer",
        "Restore",
        "Status"
    )]
    [string]$Mode = "Status"
)

$ErrorActionPreference = "Stop"

$BaseDir = Join-Path $env:ProgramData "Win11-OnDemand"
$BackupFile = Join-Path $BaseDir "backup.json"

$Groups = @{
    VMware = @(
        "VMnetDHCP",
        "VMware NAT Service"
    )

    Acer = @(
        "AcerCCAgentSvis",
        "AcerDIAgentSvis",
        "AcerEZSvc"
    )
}

$ManagedTasks = @(
    @{
        TaskPath = "\"
        TaskName = "DelayStartCareCenter2"
    },
    @{
        TaskPath = "\"
        TaskName = "DelayStartDeviceInfo2"
    }
)

function Write-Title([string]$Text) {
    Write-Host ""
    Write-Host ("=" * 70) -ForegroundColor DarkGray
    Write-Host $Text -ForegroundColor Cyan
    Write-Host ("=" * 70) -ForegroundColor DarkGray
}

function Ensure-BaseDir {
    if (-not (Test-Path $BaseDir)) {
        New-Item -Path $BaseDir -ItemType Directory -Force | Out-Null
    }
}

function Get-ServiceInfo([string]$Name) {
    $escaped = $Name.Replace("'","''")
    $cim = Get-CimInstance Win32_Service -Filter "Name='$escaped'" -ErrorAction SilentlyContinue
    if (-not $cim) { return $null }

    [PSCustomObject]@{
        Name        = $cim.Name
        DisplayName = $cim.DisplayName
        StartMode   = $cim.StartMode
        State       = $cim.State
    }
}

function Convert-StartModeToSetService([string]$StartMode) {
    switch ($StartMode) {
        "Auto"      { return "Automatic" }
        "Automatic" { return "Automatic" }
        "Manual"    { return "Manual" }
        "Disabled"  { return "Disabled" }
        default     { return "Manual" }
    }
}

function Save-Backup {
    Ensure-BaseDir

    if (Test-Path $BackupFile) {
        Write-Host "Ya existe una copia de seguridad: $BackupFile" -ForegroundColor Yellow
        Write-Host "No se sobrescribirá." -ForegroundColor Yellow
        return
    }

    $serviceNames = @($Groups.VMware + $Groups.Acer | Select-Object -Unique)
    $services = foreach ($name in $serviceNames) {
        $info = Get-ServiceInfo $name
        if ($info) { $info }
    }

    $tasks = foreach ($t in $ManagedTasks) {
        $task = Get-ScheduledTask -TaskPath $t.TaskPath -TaskName $t.TaskName -ErrorAction SilentlyContinue
        if ($task) {
            [PSCustomObject]@{
                TaskPath = $task.TaskPath
                TaskName = $task.TaskName
                Enabled  = ($task.State -ne "Disabled")
            }
        }
    }

    $backup = [PSCustomObject]@{
        Created  = (Get-Date).ToString("o")
        Services = @($services)
        Tasks    = @($tasks)
    }

    $backup | ConvertTo-Json -Depth 6 | Set-Content -Path $BackupFile -Encoding UTF8
    Write-Host "Copia de seguridad creada:" -ForegroundColor Green
    Write-Host "  $BackupFile"
}

function Set-ServiceOnDemandDisabled([string]$Name) {
    $svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "No encontrado: $Name" -ForegroundColor DarkGray
        return
    }

    if ($svc.Status -ne "Stopped") {
        try {
            Stop-Service -Name $Name -Force -ErrorAction Stop
            Write-Host "Detenido: $Name"
        }
        catch {
            Write-Host "No se pudo detener $Name : $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }

    try {
        Set-Service -Name $Name -StartupType Disabled -ErrorAction Stop
        Write-Host "Inicio automático bloqueado: $Name" -ForegroundColor Green
    }
    catch {
        Write-Host "No se pudo cambiar $Name : $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

function Start-OnDemandService([string]$Name) {
    $svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "No encontrado: $Name" -ForegroundColor DarkGray
        return
    }

    try {
        Set-Service -Name $Name -StartupType Manual -ErrorAction Stop
        Start-Service -Name $Name -ErrorAction Stop
        Write-Host "Iniciado: $Name" -ForegroundColor Green
    }
    catch {
        Write-Host "No se pudo iniciar $Name : $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

function Stop-OnDemandService([string]$Name) {
    $svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if (-not $svc) {
        Write-Host "No encontrado: $Name" -ForegroundColor DarkGray
        return
    }

    try {
        if ($svc.Status -ne "Stopped") {
            Stop-Service -Name $Name -Force -ErrorAction Stop
        }
        Set-Service -Name $Name -StartupType Disabled -ErrorAction Stop
        Write-Host "Detenido y bloqueado al arranque: $Name" -ForegroundColor Green
    }
    catch {
        Write-Host "No se pudo detener/configurar $Name : $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

function Disable-ManagedTasks {
    foreach ($t in $ManagedTasks) {
        $task = Get-ScheduledTask -TaskPath $t.TaskPath -TaskName $t.TaskName -ErrorAction SilentlyContinue
        if ($task) {
            try {
                Disable-ScheduledTask -TaskPath $t.TaskPath -TaskName $t.TaskName -ErrorAction Stop | Out-Null
                Write-Host "Tarea deshabilitada: $($t.TaskName)" -ForegroundColor Green
            }
            catch {
                Write-Host "No se pudo deshabilitar $($t.TaskName)" -ForegroundColor Yellow
            }
        }
    }
}

function Enable-AcerManagedTasks {
    foreach ($t in $ManagedTasks) {
        $task = Get-ScheduledTask -TaskPath $t.TaskPath -TaskName $t.TaskName -ErrorAction SilentlyContinue
        if ($task) {
            try {
                Enable-ScheduledTask -TaskPath $t.TaskPath -TaskName $t.TaskName -ErrorAction Stop | Out-Null
                Write-Host "Tarea habilitada temporalmente: $($t.TaskName)"
            }
            catch {
                Write-Host "No se pudo habilitar $($t.TaskName)" -ForegroundColor Yellow
            }
        }
    }
}

function Restore-Backup {
    if (-not (Test-Path $BackupFile)) {
        Write-Host "No existe copia de seguridad en $BackupFile" -ForegroundColor Red
        return
    }

    $backup = Get-Content $BackupFile -Raw | ConvertFrom-Json

    Write-Title "Restaurando servicios"

    foreach ($item in $backup.Services) {
        $svc = Get-Service -Name $item.Name -ErrorAction SilentlyContinue
        if (-not $svc) { continue }

        $startup = Convert-StartModeToSetService $item.StartMode

        try {
            Set-Service -Name $item.Name -StartupType $startup -ErrorAction Stop

            if ($item.State -eq "Running") {
                Start-Service -Name $item.Name -ErrorAction SilentlyContinue
            }
            elseif ($item.State -eq "Stopped") {
                Stop-Service -Name $item.Name -Force -ErrorAction SilentlyContinue
            }

            Write-Host "Restaurado: $($item.Name) -> $($item.StartMode) / $($item.State)" -ForegroundColor Green
        }
        catch {
            Write-Host "Error restaurando $($item.Name): $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }

    Write-Title "Restaurando tareas"

    foreach ($item in $backup.Tasks) {
        try {
            if ($item.Enabled) {
                Enable-ScheduledTask -TaskPath $item.TaskPath -TaskName $item.TaskName -ErrorAction Stop | Out-Null
            }
            else {
                Disable-ScheduledTask -TaskPath $item.TaskPath -TaskName $item.TaskName -ErrorAction Stop | Out-Null
            }

            Write-Host "Restaurada: $($item.TaskName)" -ForegroundColor Green
        }
        catch {
            Write-Host "Error restaurando $($item.TaskName)" -ForegroundColor Yellow
        }
    }

    Write-Host ""
    Write-Host "Restauración terminada. Reinicia Windows." -ForegroundColor Cyan
}

function Show-Status {
    Write-Title "Servicios VMware"
    foreach ($name in $Groups.VMware) {
        $info = Get-ServiceInfo $name
        if ($info) {
            "{0,-26} Estado={1,-10} Inicio={2}" -f $info.Name, $info.State, $info.StartMode
        } else {
            "{0,-26} No instalado" -f $name
        }
    }

    Write-Title "Utilidades Acer administradas"
    foreach ($name in $Groups.Acer) {
        $info = Get-ServiceInfo $name
        if ($info) {
            "{0,-26} Estado={1,-10} Inicio={2}" -f $info.Name, $info.State, $info.StartMode
        } else {
            "{0,-26} No instalado" -f $name
        }
    }

    Write-Title "Servicios Acer protegidos (NO modificados)"
    foreach ($name in @("AcerDeviceEnablingServiceV2","AcerQAAgentSvis","ASMSvc","AcerServiceSvc")) {
        $info = Get-ServiceInfo $name
        if ($info) {
            "{0,-30} Estado={1,-10} Inicio={2}" -f $info.Name, $info.State, $info.StartMode
        }
    }

    Write-Title "Tareas Acer administradas"
    foreach ($t in $ManagedTasks) {
        $task = Get-ScheduledTask -TaskPath $t.TaskPath -TaskName $t.TaskName -ErrorAction SilentlyContinue
        if ($task) {
            "{0,-32} Estado={1}" -f $task.TaskName, $task.State
        }
    }
}

switch ($Mode) {

    "Optimize" {
        Write-Title "Guardando configuración original"
        Save-Backup

        Write-Title "VMware: solo bajo demanda"
        foreach ($name in $Groups.VMware) {
            Set-ServiceOnDemandDisabled $name
        }

        Write-Title "Acer: utilidades no esenciales solo bajo demanda"
        foreach ($name in $Groups.Acer) {
            Set-ServiceOnDemandDisabled $name
        }

        Write-Title "Evitando relanzamiento automático de utilidades Acer"
        Disable-ManagedTasks

        Write-Title "Componentes que se conservaron"
        Write-Host "AcerDeviceEnablingServiceV2  -> sin cambios"
        Write-Host "AcerQAAgentSvis             -> sin cambios"
        Write-Host "ASMSvc                      -> sin cambios"
        Write-Host "AcerServiceSvc              -> sin cambios"
        Write-Host "Realtek / audio             -> sin cambios"
        Write-Host "AMD graphics                -> sin cambios"
        Write-Host "Windows Search              -> sin cambios"
        Write-Host "Defender / Firewall         -> sin cambios"
        Write-Host "Windows Update              -> sin cambios"
        Write-Host "Wi-Fi / Bluetooth / input   -> sin cambios"

        Write-Host ""
        Write-Host "Optimización aplicada. Reinicia Windows y vuelve a medir." -ForegroundColor Cyan
    }

    "Start-VMware" {
        Write-Title "Iniciando red VMware"
        foreach ($name in $Groups.VMware) {
            Start-OnDemandService $name
        }
        Write-Host ""
        Write-Host "Ahora puedes abrir VMware Workstation." -ForegroundColor Cyan
    }

    "Stop-VMware" {
        Write-Title "Deteniendo red VMware"
        foreach ($name in [array]($Groups.VMware | Select-Object -Reverse)) {
            Stop-OnDemandService $name
        }
    }

    "Start-Acer" {
        Write-Title "Iniciando utilidades Acer"
        Enable-AcerManagedTasks
        foreach ($name in [array]($Groups.Acer | Select-Object -Reverse)) {
            Start-OnDemandService $name
        }
        Write-Host ""
        Write-Host "Las utilidades Acer seleccionadas están disponibles hasta que ejecutes Stop-Acer." -ForegroundColor Cyan
    }

    "Stop-Acer" {
        Write-Title "Deteniendo utilidades Acer"
        foreach ($name in $Groups.Acer) {
            Stop-OnDemandService $name
        }
        Disable-ManagedTasks
    }

    "Restore" {
        Restore-Backup
    }

    "Status" {
        Show-Status
    }
}
