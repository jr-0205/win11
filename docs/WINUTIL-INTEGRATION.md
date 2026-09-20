# Integración WinUtil

## Objetivo

Aprovechar el catálogo y la experiencia acumulada por Chris Titus Tech WinUtil sin ejecutar código remoto ni asumir que un tweak genérico es apropiado para este equipo.

## Fuente fijada

- Release: 26.08.19
- Commit: 086aecf4b7d165f9fd1822049435c418a48e7cba
- Repositorio: https://github.com/ChrisTitusTech/winutil

La aplicación consulta exclusivamente rutas raw asociadas a ese commit.

## Lo que se importa

- config/tweaks.json
- config/preset.json

## Lo que NO se importa ni ejecuta

- winutil.ps1
- irm/iex
- InvokeScript
- comandos arbitrarios de PowerShell
- cambios de Registro definidos por WinUtil
- cambios de servicios definidos por WinUtil
- eliminación de AppX
- eliminación de Edge, OneDrive u otros componentes

## Motivo

Un preset general puede contener cambios que no sean apropiados para la configuración concreta del usuario. Por ejemplo, el tweak de servicios de WinUtil puede modificar servicios como SharedAccess, mientras Windows11Optimizer conserva funciones que podrían depender de esos servicios.

Por ello, el catálogo es una fuente de investigación y no un motor de ejecución.

## Caché

Los JSON descargados se guardan en:

C:\ProgramData\Windows11Optimizer\winutil\26.08.19

Al pulsar "Actualizar caché", se vuelven a descargar los mismos archivos del mismo commit fijado. No se cambia automáticamente a una release más nueva.

## Futuras integraciones

Para portar un tweak de WinUtil a Windows11Optimizer:

1. Revisar manualmente su comportamiento.
2. Identificar todas las claves, servicios, tareas y comandos que modifica.
3. Definir un backup del estado real del equipo.
4. Implementarlo nativamente en C# cuando sea posible.
5. Añadir una operación de restauración.
6. Probar el cambio en una rama separada.
7. Solo después añadirlo a un perfil de usuario.

Nunca debe copiarse automáticamente un preset completo al perfil seguro.
