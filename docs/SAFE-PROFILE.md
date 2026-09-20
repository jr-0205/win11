# Perfil seguro

El perfil seguro está diseñado para un portátil Windows 11 que debe conservar funciones de comodidad y hardware.

## Servicios administrados

### VMware

| Servicio | Modo optimizado | Motivo |
|---|---|---|
| `VMnetDHCP` | Disabled cuando VMware no se usa | DHCP para redes virtuales VMware |
| `VMware NAT Service` | Disabled cuando VMware no se usa | NAT para redes virtuales VMware |

Al iniciar VMware desde la aplicación, ambos se cambian a `Manual` y se inician.

### Acer bajo demanda

| Servicio | Modo optimizado |
|---|---|
| `AcerCCAgentSvis` | Disabled |
| `AcerDIAgentSvis` | Disabled |
| `AcerEZSvc` | Disabled |

### Acer protegido

Estos componentes no se modifican automáticamente:

- `AcerDeviceEnablingServiceV2`
- `AcerQAAgentSvis`
- `ASMSvc`
- `AcerServiceSvc`

El principio es conservarlos mientras no se haya demostrado que pueden retirarse sin afectar Fn, modos de energía, batería, hardware o funciones propias del portátil.

## Tareas administradas

El perfil seguro puede deshabilitar:

- `\DelayStartCareCenter2`
- `\DelayStartDeviceInfo2`

Su estado original se guarda antes del cambio.
