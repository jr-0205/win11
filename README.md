# Windows 11 Optimizer

Aplicación de escritorio para **medir, reducir y controlar el consumo en segundo plano de Windows 11 sin eliminar funciones esenciales**.

La prioridad del proyecto es la reversibilidad: antes de modificar servicios administrados, la aplicación guarda su estado original y permite restaurarlo.

## Estado

**v0.5**

### Incluido

- Panel con CPU, RAM y cantidad de procesos.
- Perfil de optimización segura.
- Copia de seguridad automática en `%ProgramData%\Windows11Optimizer\backup.json`.
- Restauración de la configuración original.
- VMware bajo demanda:
  - `VMnetDHCP`
  - `VMware NAT Service`
- Utilidades Acer bajo demanda:
  - `AcerCCAgentSvis`
  - `AcerDIAgentSvis`
  - `AcerEZSvc`
- Protección explícita de componentes Acer que pueden intervenir en funciones del portátil:
  - `AcerDeviceEnablingServiceV2`
  - `AcerQAAgentSvis`
  - `ASMSvc`
  - `AcerServiceSvc`
- Inventario de programas registrados para iniciar con Windows.
- Acceso directo a la pantalla oficial de Apps de inicio.
- Exportación de diagnóstico a TXT.
- Registro de operaciones dentro de la aplicación.
- Catálogo de referencia de Chris Titus Tech WinUtil.
- Generador de EXE portable e instalador.\n- Administrador seguro de servicios por allowlist (Manual/Automático/Iniciar/Detener).\n- Selector reversible del modo de virtualización para alternar entre modo normal y modo VMware, con copia de seguridad y reinicio controlado.\n- Interfaz simplificada para usuarios no técnicos.\n- Las 66 opciones del catálogo fijado de WinUtil se muestran en español con explicaciones sencillas.

## Integración con WinUtil

Windows11Optimizer **no ejecuta**:

```powershell
irm https://christitus.com/win | iex
```

La integración de v0.2 es deliberadamente de **solo lectura**:

1. Usa WinUtil estable `26.08.19`.
2. Fija el commit `086aecf4b7d165f9fd1822049435c418a48e7cba`.
3. Descarga únicamente:
   - `config/tweaks.json`
   - `config/preset.json`
4. Guarda una caché en `%ProgramData%\Windows11Optimizer\winutil\26.08.19`.
5. Muestra nombre, descripción, presets, tipo de acciones y nivel de revisión.
6. No ejecuta `InvokeScript`, no cambia el Registro y no aplica servicios definidos por WinUtil.

Esto permite investigar y comparar reglas de WinUtil sin convertir la aplicación en un lanzador de código remoto.

Los avisos y la licencia MIT de WinUtil se conservan en `THIRD-PARTY-NOTICES.md`.

## Qué NO hace el perfil seguro

No desactiva:

- Microsoft Defender
- Firewall
- Windows Update
- Windows Search
- SysMain
- Audio Realtek
- Wi-Fi
- Bluetooth
- Touchpad/entrada de texto
- AMD Graphics
- Acer Quick Access / servicios Acer protegidos

El objetivo no es conseguir el menor número posible de procesos a cualquier costo. El objetivo es reducir actividad innecesaria manteniendo las funciones útiles del equipo.

## Requisitos para desarrollar

- Windows 11 x64
- .NET 8 SDK
- VS Code, Visual Studio 2022 o terminal
- Permisos de administrador al ejecutar la aplicación

## Generar el EXE desde VS Code

Abre una terminal PowerShell en la carpeta raíz del repositorio.

### Opción recomendada: EXE portable

```powershell
.\exe-generated.ps1 -Clean -PortableOnly
```

Resultado:

```text
artifacts\portable\Windows11Optimizer.exe
```

El EXE es `self-contained`: el equipo donde se ejecute no necesita tener instalado .NET 8 Desktop Runtime.

### Generar instalador

El instalador usa **Inno Setup 6**.

Si ya tienes Inno Setup:

```powershell
.\exe-generated.ps1 -Clean
```

Resultado:

```text
artifacts\installer\Windows11Optimizer-Setup-x64.exe
```

Si no tienes Inno Setup y quieres que el script lo instale mediante WinGet:

```powershell
.\exe-generated.ps1 -Clean -InstallInno
```

También puedes hacer doble clic en:

```text
exe-generated.cmd
```

El script nunca instala Inno Setup por sí solo. Solo lo hace cuando se proporciona explícitamente `-InstallInno`.

## Compilar manualmente

```powershell
dotnet restore .\Windows11Optimizer.sln
dotnet build .\Windows11Optimizer.sln -c Release
```

Publicación manual:

```powershell
dotnet publish .\src\Windows11Optimizer\Windows11Optimizer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\artifacts\publish
```

## GitHub Actions

Cada push a `main` ejecuta el mismo generador:

```powershell
.\exe-generated.ps1 -Clean -PortableOnly
```

Si compila correctamente, GitHub Actions publica el artifact:

```text
Windows11Optimizer-v0.2-win-x64
```

## Funcionamiento del modo bajo demanda

Cuando se pulsa **Detener VMware** o se aplica el perfil seguro:

1. El servicio se detiene.
2. Su inicio queda `Disabled`, por lo que no arrancará con Windows.

Cuando se pulsa **Iniciar VMware**:

1. El inicio cambia temporalmente a `Manual`.
2. El servicio se inicia.

Al terminar, se puede volver a detener desde la aplicación.

El mismo concepto se aplica a las utilidades Acer seleccionadas.

## Backup

La primera aplicación del perfil seguro crea:

```text
C:\ProgramData\Windows11Optimizer\backup.json
```

El backup conserva:

- tipo de inicio original de cada servicio modificado;
- si el servicio estaba ejecutándose;
- estado de las tareas programadas administradas.

La aplicación no sobrescribe automáticamente ese primer backup.

## Scripts auxiliares

La carpeta `tools/` contiene los prototipos PowerShell usados durante el desarrollo:

- `Diagnostico_Windows11.ps1`
- `Win11_OnDemand.ps1`

## Roadmap

- [ ] Historial antes/después por perfil.
- [ ] Administrador reversible de aplicaciones de inicio desde la propia interfaz.
- [ ] Análisis de servicios de terceros detectados automáticamente.
- [ ] Perfiles `Equilibrado`, `Mínimo` y `Desarrollo`.
- [ ] Inicio automático de servicios al abrir aplicaciones asociadas.
- [ ] Reglas para Docker Desktop y otras herramientas de desarrollo.
- [ ] Medición de disco y tiempo de arranque.
- [ ] Detección de dependencias antes de modificar un servicio.
- [ ] Revisión individual de reglas WinUtil antes de portar cualquier tweak a implementación nativa.

## Seguridad

No agregues servicios de Windows al perfil de desactivación sin comprobar dependencias y efectos. Un nombre de servicio desconocido debe tratarse como **conservar** hasta investigarlo.

Los datos de WinUtil importados son referencia; el nivel mostrado por la interfaz **no es una garantía de seguridad**.

## Licencias

Windows11Optimizer: MIT.

WinUtil: MIT, Copyright (c) 2022 CT Tech Group LLC. Consulta `THIRD-PARTY-NOTICES.md`.


## Análisis inteligente y Autoruns

Windows11Optimizer v0.5 añade medición antes/después de la optimización y una integración opcional con Microsoft Sysinternals Autoruns/Autorunsc.

- La app funciona aunque Autoruns no esté instalado.
- Si `Autorunsc64.exe` o `Autorunsc.exe` está disponible, puede analizar ubicaciones de autoarranque adicionales.
- Autoruns no se distribuye dentro del instalador de Windows11Optimizer.
- El botón **Obtener Autoruns** abre la página oficial de Microsoft Sysinternals.
- Autorunsc se usa únicamente para lectura/análisis; Windows11Optimizer no le delega cambios del sistema.

## WinUtil simplificado

La vista de WinUtil muestra primero opciones que Windows11Optimizer ha portado de forma nativa y reversible.

Opciones aplicables en v0.5:

- Mostrar extensiones de archivo.
- Mostrar archivos ocultos.
- Habilitar “Finalizar tarea” desde la barra de tareas.
- Mostrar el acceso de búsqueda de la barra de tareas.
- Activar el tema oscuro de Windows.

Antes de aplicar cada ajuste se guarda el valor actual. El botón **Deshacer** restaura ese valor.

Las demás opciones del catálogo fijado siguen disponibles mediante **Mostrar opciones avanzadas y de consulta**, pero no ejecutan scripts remotos ni `irm | iex`.


## Modos de virtualización v0.6

La sección **Máquinas virtuales** distingue dos perfiles:

### Modo normal

Configura `hypervisorlaunchtype=auto` para dejar disponible el hipervisor de Windows. Está pensado para escenarios que dependen de la virtualización de Windows, como Docker Desktop con WSL2/Hyper-V, Hyper-V, Windows Sandbox y funciones VBS cuando estén configuradas.

### Modo VMware

Configura `hypervisorlaunchtype=off` para el próximo arranque y prepara los servicios principales de VMware:

- `VMAuthdService`
- `VMnetDHCP`
- `VMware NAT Service`

Opcionalmente puede activar:

- `VMUSBArbService` para paso de dispositivos USB.
- `VMwareAutostartService` para autoinicio de máquinas virtuales configuradas.

La aplicación **no elimina ni modifica las políticas de VBS, Integridad de memoria o Credential Guard** al cambiar a Modo VMware. Solo cambia el arranque del hipervisor mediante BCD. La pantalla consulta de forma independiente el estado de VBS e Integridad de memoria y sigue detectando si hace falta reiniciar.

Los estados originales de los servicios administrados se incorporan al backup para poder restaurarlos.


## Optimización inteligente v0.7

La interfaz principal se simplificó para que el usuario no tenga que administrar servicios o tareas por nombre.

### Optimización

El módulo inteligente:

- analiza únicamente componentes conocidos por una allowlist;
- detecta utilidades Acer, VMware y Docker que se inician siempre con Windows;
- recomienda dejarlos disponibles para que una aplicación los inicie cuando los necesite;
- usa inicio Manual en lugar de Disabled;
- no fuerza el cierre de componentes que ya están funcionando;
- no deshabilita actividades programadas automáticamente;
- conserva una copia del estado anterior;
- permite deshacer cambios;
- detecta entradas antiguas del inicio de Windows y las envía a revisión.

### Máquinas virtuales

Solo hay dos elecciones principales:

- **Windows y Docker**: prioriza Docker Desktop, WSL2, Windows Sandbox y las funciones de virtualización de Windows.
- **VMware**: prioriza VMware, máquinas virtuales y compatibilidad con virtualización anidada cuando el modo de virtualización de Windows interfiera.

Los servicios VMware se administran internamente; ya no existe una pantalla separada para iniciarlos o detenerlos uno por uno.

### Ajustes

La antigua vista completa de WinUtil dejó de mostrarse al usuario.

La pantalla **Ajustes** enseña únicamente opciones que Windows11Optimizer ha portado de forma nativa, reversible y comprensible.
