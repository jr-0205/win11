# Windows 11 - Diagnostico de rendimiento (solo lectura)
# No modifica servicios, registro, inicio ni configuraciones.
# Genera un reporte TXT en el Escritorio.

$ErrorActionPreference = "SilentlyContinue"

# ---------- Configuracion ----------
$Samples = 15
$IntervalSeconds = 2
$TopProcesses = 20

$Desktop = [Environment]::GetFolderPath("Desktop")
$Stamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$Report = Join-Path $Desktop "Diagnostico_Windows11_$Stamp.txt"

# ---------- Funciones ----------
function Sanitize-Text {
    param([AllowNull()][object]$Value)
    if ($null -eq $Value) { return "" }

    $s = [string]$Value
    if ($env:USERPROFILE) {
        $s = $s.Replace($env:USERPROFILE, "%USERPROFILE%")
    }
    if ($env:USERNAME) {
        $s = $s.Replace($env:USERNAME, "%USERNAME%")
    }
    if ($env:COMPUTERNAME) {
        $s = $s.Replace($env:COMPUTERNAME, "%COMPUTERNAME%")
    }
    return $s
}

function Add-Line {
    param([string]$Text = "")
    Add-Content -Path $Report -Value $Text -Encoding UTF8
}

function Add-Section {
    param([string]$Title)
    Add-Line ""
    Add-Line ("=" * 78)
    Add-Line $Title
    Add-Line ("=" * 78)
}

function Format-Bytes {
    param([double]$Bytes)
    if ($Bytes -ge 1TB) { return "{0:N2} TB" -f ($Bytes / 1TB) }
    if ($Bytes -ge 1GB) { return "{0:N2} GB" -f ($Bytes / 1GB) }
    if ($Bytes -ge 1MB) { return "{0:N2} MB" -f ($Bytes / 1MB) }
    if ($Bytes -ge 1KB) { return "{0:N2} KB" -f ($Bytes / 1KB) }
    return "{0:N0} B" -f $Bytes
}

# ---------- Encabezado ----------
"DIAGNOSTICO DE RENDIMIENTO - WINDOWS 11" | Set-Content -Path $Report -Encoding UTF8
Add-Line ("Fecha: " + (Get-Date -Format "yyyy-MM-dd HH:mm:ss"))
Add-Line "Modo: SOLO LECTURA - este script no cambia ninguna configuracion."
Add-Line "Privacidad: nombre de usuario, perfil y nombre del equipo se reemplazan."

# ---------- Sistema ----------
Add-Section "1. SISTEMA"

$os = Get-CimInstance Win32_OperatingSystem
$cs = Get-CimInstance Win32_ComputerSystem
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1

$boot = $os.LastBootUpTime
$uptime = (Get-Date) - $boot

Add-Line ("Windows: {0} {1}" -f $os.Caption, $os.OSArchitecture)
Add-Line ("Version: {0}" -f $os.Version)
Add-Line ("Build: {0}" -f $os.BuildNumber)
Add-Line ("CPU: {0}" -f (Sanitize-Text $cpu.Name))
Add-Line ("Nucleos fisicos: {0}" -f $cpu.NumberOfCores)
Add-Line ("Procesadores logicos: {0}" -f $cpu.NumberOfLogicalProcessors)
Add-Line ("RAM fisica instalada: {0:N2} GB" -f ($cs.TotalPhysicalMemory / 1GB))
Add-Line ("Ultimo arranque: {0}" -f $boot)
Add-Line ("Tiempo encendido: {0} dias, {1} h, {2} min" -f $uptime.Days, $uptime.Hours, $uptime.Minutes)

$activePlan = (powercfg /getactivescheme 2>$null | Out-String).Trim()
if ($activePlan) {
    Add-Line ("Plan de energia: " + (Sanitize-Text $activePlan))
}

# ---------- Estado actual ----------
Add-Section "2. ESTADO ACTUAL DE MEMORIA Y PROCESOS"

$totalRam = [double]$cs.TotalPhysicalMemory
$freeRam = [double]$os.FreePhysicalMemory * 1KB
$usedRam = $totalRam - $freeRam
$usedPct = if ($totalRam -gt 0) { ($usedRam / $totalRam) * 100 } else { 0 }

$processes = Get-Process
$processCount = @($processes).Count
$threadCount = ($processes | Measure-Object -Property Threads -Sum).Sum
$handleCount = ($processes | Measure-Object -Property HandleCount -Sum).Sum

Add-Line ("RAM usada: {0} / {1} ({2:N1} %)" -f (Format-Bytes $usedRam), (Format-Bytes $totalRam), $usedPct)
Add-Line ("RAM disponible: {0}" -f (Format-Bytes $freeRam))
Add-Line ("Procesos: {0}" -f $processCount)
Add-Line ("Hilos totales aproximados: {0}" -f $threadCount)
Add-Line ("Handles totales aproximados: {0}" -f $handleCount)

# ---------- Muestreo ----------
Add-Section "3. MUESTREO EN REPOSO"
Add-Line ("Muestras: {0}, intervalo: {1} s, duracion aprox.: {2} s" -f $Samples, $IntervalSeconds, ($Samples * $IntervalSeconds))
Add-Line "Durante esta parte no uses el equipo para que los datos representen reposo real."

$cpuSamples = @()
$diskBusySamples = @()
$diskReadSamples = @()
$diskWriteSamples = @()
$diskQueueSamples = @()
$availMemSamples = @()

for ($i = 1; $i -le $Samples; $i++) {
    $p = Get-CimInstance Win32_PerfFormattedData_PerfOS_Processor -Filter "Name='_Total'"
    $d = Get-CimInstance Win32_PerfFormattedData_PerfDisk_PhysicalDisk -Filter "Name='_Total'"
    $m = Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory

    if ($p) { $cpuSamples += [double]$p.PercentProcessorTime }
    if ($d) {
        $diskBusySamples += [double]$d.PercentDiskTime
        $diskReadSamples += [double]$d.DiskReadBytesPersec
        $diskWriteSamples += [double]$d.DiskWriteBytesPersec
        $diskQueueSamples += [double]$d.CurrentDiskQueueLength
    }
    if ($m) { $availMemSamples += ([double]$m.AvailableMBytes * 1MB) }

    Start-Sleep -Seconds $IntervalSeconds
}

function Stat-Line {
    param(
        [string]$Name,
        [array]$Values,
        [string]$Suffix = ""
    )
    if (-not $Values -or $Values.Count -eq 0) {
        Add-Line ("{0}: sin datos" -f $Name)
        return
    }

    $measure = $Values | Measure-Object -Average -Minimum -Maximum
    Add-Line ("{0}: promedio {1:N2}{4} | min {2:N2}{4} | max {3:N2}{4}" -f `
        $Name, $measure.Average, $measure.Minimum, $measure.Maximum, $Suffix)
}

Stat-Line "CPU total" $cpuSamples " %"
Stat-Line "Disco ocupado" $diskBusySamples " %"
Stat-Line "Cola de disco" $diskQueueSamples ""

if ($diskReadSamples.Count -gt 0) {
    $avg = ($diskReadSamples | Measure-Object -Average).Average
    $max = ($diskReadSamples | Measure-Object -Maximum).Maximum
    Add-Line ("Lectura de disco: promedio {0}/s | max {1}/s" -f (Format-Bytes $avg), (Format-Bytes $max))
}
if ($diskWriteSamples.Count -gt 0) {
    $avg = ($diskWriteSamples | Measure-Object -Average).Average
    $max = ($diskWriteSamples | Measure-Object -Maximum).Maximum
    Add-Line ("Escritura de disco: promedio {0}/s | max {1}/s" -f (Format-Bytes $avg), (Format-Bytes $max))
}
if ($availMemSamples.Count -gt 0) {
    $avg = ($availMemSamples | Measure-Object -Average).Average
    $min = ($availMemSamples | Measure-Object -Minimum).Minimum
    Add-Line ("RAM disponible durante muestreo: promedio {0} | minimo {1}" -f (Format-Bytes $avg), (Format-Bytes $min))
}

# ---------- Procesos ----------
Add-Section "4. PROCESOS CON MAYOR CONSUMO ACTUAL"

$logicalCPU = [Math]::Max(1, [int]$cpu.NumberOfLogicalProcessors)
$perfProc = Get-CimInstance Win32_PerfFormattedData_PerfProc_Process |
    Where-Object { $_.Name -notin @("_Total", "Idle") -and $_.IDProcess -gt 0 }

$procTable = foreach ($p in $perfProc) {
    [PSCustomObject]@{
        Proceso      = $p.Name
        PID          = $p.IDProcess
        CPU_Pct      = [Math]::Round(([double]$p.PercentProcessorTime / $logicalCPU), 2)
        RAM_Priv_MB  = [Math]::Round(([double]$p.WorkingSetPrivate / 1MB), 1)
        RAM_Total_MB = [Math]::Round(([double]$p.WorkingSet / 1MB), 1)
        IO_KB_s      = [Math]::Round(([double]$p.IODataBytesPersec / 1KB), 1)
        Hilos        = $p.ThreadCount
        Handles      = $p.HandleCount
    }
}

Add-Line "Top por RAM privada:"
($procTable | Sort-Object RAM_Priv_MB -Descending | Select-Object -First $TopProcesses |
    Format-Table -AutoSize | Out-String -Width 220).TrimEnd() | Add-Content -Path $Report -Encoding UTF8

Add-Line ""
Add-Line "Top por CPU instantanea:"
($procTable | Sort-Object CPU_Pct -Descending | Select-Object -First $TopProcesses |
    Format-Table -AutoSize | Out-String -Width 220).TrimEnd() | Add-Content -Path $Report -Encoding UTF8

Add-Line ""
Add-Line "Top por E/S:"
($procTable | Sort-Object IO_KB_s -Descending | Select-Object -First $TopProcesses |
    Format-Table -AutoSize | Out-String -Width 220).TrimEnd() | Add-Content -Path $Report -Encoding UTF8

# ---------- Inicio ----------
Add-Section "5. PROGRAMAS CONFIGURADOS PARA INICIAR CON WINDOWS"

$startup = Get-CimInstance Win32_StartupCommand |
    Select-Object Name, Command, Location

if ($startup) {
    $startupSanitized = foreach ($x in $startup) {
        [PSCustomObject]@{
            Nombre   = Sanitize-Text $x.Name
            Comando  = Sanitize-Text $x.Command
            Ubicacion = Sanitize-Text $x.Location
        }
    }

    ($startupSanitized | Sort-Object Nombre |
        Format-Table -Wrap -AutoSize | Out-String -Width 240).TrimEnd() |
        Add-Content -Path $Report -Encoding UTF8
} else {
    Add-Line "No se encontraron entradas mediante Win32_StartupCommand."
}

# ---------- Servicios ----------
Add-Section "6. SERVICIOS AUTOMATICOS EN EJECUCION"

$services = Get-CimInstance Win32_Service |
    Where-Object { $_.StartMode -eq "Auto" -and $_.State -eq "Running" } |
    Select-Object Name, DisplayName, StartMode, State, PathName

$servicesSanitized = foreach ($s in $services) {
    [PSCustomObject]@{
        Nombre      = $s.Name
        Descripcion = $s.DisplayName
        Inicio      = $s.StartMode
        Estado      = $s.State
        Ruta        = Sanitize-Text $s.PathName
    }
}

Add-Line ("Cantidad: {0}" -f @($servicesSanitized).Count)
($servicesSanitized | Sort-Object Nombre |
    Format-Table -Wrap -AutoSize | Out-String -Width 260).TrimEnd() |
    Add-Content -Path $Report -Encoding UTF8

# ---------- Tareas no Microsoft ----------
Add-Section "7. TAREAS PROGRAMADAS ACTIVAS DE TERCEROS"

$thirdPartyTasks = Get-ScheduledTask |
    Where-Object {
        $_.State -ne "Disabled" -and
        $_.TaskPath -notlike "\Microsoft\*"
    } |
    Select-Object TaskName, TaskPath, State

if ($thirdPartyTasks) {
    ($thirdPartyTasks | Sort-Object TaskPath, TaskName |
        Format-Table -AutoSize | Out-String -Width 220).TrimEnd() |
        Add-Content -Path $Report -Encoding UTF8
} else {
    Add-Line "No se detectaron tareas activas fuera de \Microsoft\."
}

# ---------- Paginacion ----------
Add-Section "8. ARCHIVO DE PAGINACION"

Add-Line ("Administracion automatica: {0}" -f $cs.AutomaticManagedPagefile)

$pagefiles = Get-CimInstance Win32_PageFileUsage
if ($pagefiles) {
    foreach ($pf in $pagefiles) {
        Add-Line ("Archivo: {0}" -f (Sanitize-Text $pf.Name))
        Add-Line ("  Tamano asignado: {0:N0} MB" -f $pf.AllocatedBaseSize)
        Add-Line ("  Uso actual: {0:N0} MB" -f $pf.CurrentUsage)
        Add-Line ("  Pico de uso: {0:N0} MB" -f $pf.PeakUsage)
    }
} else {
    Add-Line "No se obtuvo informacion del archivo de paginacion."
}

# ---------- Unidades ----------
Add-Section "9. ALMACENAMIENTO"

$drives = Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3"
foreach ($drive in $drives) {
    $used = $drive.Size - $drive.FreeSpace
    $pct = if ($drive.Size -gt 0) { ($used / $drive.Size) * 100 } else { 0 }
    Add-Line ("Unidad {0} | Usado {1} / {2} ({3:N1} %) | Libre {4}" -f `
        $drive.DeviceID, (Format-Bytes $used), (Format-Bytes $drive.Size), $pct, (Format-Bytes $drive.FreeSpace))
}

# ---------- Resumen final ----------
Add-Section "10. RESUMEN PARA ANALISIS"

$cpuAvg = if ($cpuSamples.Count) { ($cpuSamples | Measure-Object -Average).Average } else { 0 }
$diskAvg = if ($diskBusySamples.Count) { ($diskBusySamples | Measure-Object -Average).Average } else { 0 }

Add-Line ("RAM en reposo: {0:N1} %" -f $usedPct)
Add-Line ("CPU promedio durante muestreo: {0:N2} %" -f $cpuAvg)
Add-Line ("Disco ocupado promedio: {0:N2} %" -f $diskAvg)
Add-Line ("Procesos: {0}" -f $processCount)
Add-Line ("Hilos: {0}" -f $threadCount)
Add-Line ("Handles: {0}" -f $handleCount)
Add-Line ("Entradas de inicio detectadas: {0}" -f @($startup).Count)
Add-Line ("Servicios automaticos ejecutandose: {0}" -f @($servicesSanitized).Count)
Add-Line ("Tareas programadas de terceros activas: {0}" -f @($thirdPartyTasks).Count)

Add-Line ""
Add-Line "FIN DEL REPORTE"
Add-Line "Adjunta este archivo en ChatGPT para analizarlo."

Write-Host ""
Write-Host "Reporte generado correctamente:" -ForegroundColor Green
Write-Host $Report -ForegroundColor Cyan
Write-Host ""
Write-Host "El script no modifico ninguna configuracion." -ForegroundColor Green
