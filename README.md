# Windows 11 Optimizer

Aplicación de escritorio para **medir, reducir y controlar el consumo en segundo plano de Windows 11 sin eliminar funciones esenciales**.

La prioridad del proyecto es la reversibilidad: antes de modificar servicios administrados, la aplicación guarda su estado original y permite restaurarlo.

## Estado

**v0.1 – prototipo funcional**

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

## Requisitos

- Windows 11 x64
- .NET 8 Desktop Runtime, o publicar como `self-contained`
- Permisos de administrador para modificar servicios y tareas
- Visual Studio 2022 o .NET 8 SDK para compilar

## Compilar

```powershell
dotnet restore .\Windows11Optimizer.sln
dotnet build .\Windows11Optimizer.sln -c Release
```

### Publicar como ejecutable para Windows x64

```powershell
dotnet publish .\src\Windows11Optimizer\Windows11Optimizer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish
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
- [ ] Empaquetado MSIX / instalador.

## Seguridad

No agregues servicios de Windows al perfil de desactivación sin comprobar dependencias y efectos. Un nombre de servicio desconocido debe tratarse como **conservar** hasta investigarlo.

## Licencia

MIT.
