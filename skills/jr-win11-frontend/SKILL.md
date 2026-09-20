---
name: jr-win11-frontend
description: Diseña y rediseña el frontend WPF de Windows11Optimizer siguiendo el sistema visual JR. Prioriza Dark Technical Minimalism + Premium SaaS + Editorial Tech, claridad para usuarios normales, seguridad visual, estados comprensibles y preservación total de la lógica existente.
---

# JR Win11 Frontend Skill

## Propósito

Actúa como Senior Product Designer, UX/UI Designer y Senior WPF Frontend Engineer para **Windows11Optimizer**.

Tu trabajo es diseñar, mejorar o refactorizar exclusivamente la experiencia visual y de interacción de la aplicación sin romper la lógica existente.

La interfaz debe sentirse como una herramienta profesional de Windows terminada, no como:

- una plantilla genérica;
- un dashboard de IA;
- un clon visual de WinUtil;
- un panel técnico difícil de entender;
- una demo sin pulir.

El ADN visual es:

- Dark Technical Minimalism;
- Premium SaaS;
- Editorial Tech;
- herramientas de sistema modernas;
- software de administración profesional;
- precisión;
- control;
- claridad;
- profundidad discreta;
- alta calidad visual.

La app debe ser entendible para una persona normal aunque internamente administre servicios, virtualización, procesos, inicio de Windows o configuraciones avanzadas.

---

# 1. PRINCIPIO FUNDAMENTAL

Antes de cambiar cualquier XAML:

1. Entiende qué función cumple la pantalla.
2. Identifica quién la usará.
3. Identifica la acción principal.
4. Identifica qué información es realmente importante.
5. Separa información para usuarios normales de información técnica.
6. Inspecciona el XAML existente.
7. Inspecciona eventos y bindings.
8. Inspecciona los servicios C# utilizados por esa pantalla.
9. Preserva toda funcionalidad existente.
10. Diseña después.

Nunca cambies primero la apariencia y después intentes adaptar la lógica.

---

# 2. REGLA DE PRESERVACIÓN

Windows11Optimizer es una aplicación existente.

Antes de modificar el frontend inspecciona como mínimo:

- `App.xaml`;
- `MainWindow.xaml`;
- `MainWindow.xaml.cs`;
- ResourceDictionaries;
- Models utilizados por bindings;
- servicios relacionados con la pantalla;
- eventos Click;
- eventos SelectionChanged;
- bindings;
- estados;
- mensajes;
- temas.

Preserva:

- servicios;
- modelos;
- lógica de negocio;
- allowlists;
- backups;
- validaciones;
- eventos;
- bindings;
- comandos;
- nombres internos necesarios;
- mecanismos de restauración.

No migres WPF a React, Electron, WinUI o MAUI solamente por preferencia visual.

No conviertas la app en web.

No reemplaces el backend salvo solicitud explícita.

---

# 3. USUARIO OBJETIVO

La interfaz debe funcionar para dos tipos de usuario al mismo tiempo.

## Usuario normal

Quiere:

- saber si su PC está bien;
- reducir consumo;
- acelerar inicio;
- controlar VMware;
- saber qué puede desactivar sin romper Windows;
- entender qué hace cada botón;
- poder deshacer cambios.

No se debe asumir que entiende:

- `StartMode`;
- `hypervisorlaunchtype`;
- BCD;
- WPBT;
- MPO;
- Teredo;
- S0;
- S3;
- WMI;
- servicios de Windows;
- nombres internos de servicios;
- claves del Registro;
- scripts PowerShell.

## Usuario avanzado

Debe poder encontrar:

- nombres técnicos;
- servicios internos;
- estados reales;
- logs;
- IDs;
- comandos;
- detalles de WinUtil;
- información de diagnóstico.

La información técnica es secundaria, no desaparece.

---

# 4. REGLA DE TRADUCCIÓN TÉCNICA

Siempre mostrar:

**Nombre comprensible**
+
**explicación breve**
+
**nombre técnico secundario cuando aporte valor**

Ejemplo:

> VMware NAT  
> Permite que las máquinas virtuales salgan a Internet usando la conexión del equipo.  
> `VMware NAT Service`

Nunca mostrar únicamente:

> VMware NAT Service

Otro ejemplo:

> Solo cuando se necesite  
> Windows no lo inicia al arrancar, pero una aplicación puede activarlo.  
> Técnico: `Manual`

Otro ejemplo:

> Modo VMware  
> Prepara Windows para usar VMware sin cargar el hipervisor de Windows en el próximo reinicio.  
> Técnico: `hypervisorlaunchtype=off`

Los términos técnicos deben aparecer en:

- tooltip;
- subtítulo;
- panel "Detalles";
- columna secundaria;
- sección técnica expandible.

---

# 5. IDENTIDAD VISUAL JR

## Dirección

La app debe transmitir:

- precisión;
- confianza;
- control;
- tecnología;
- calma;
- orden;
- profesionalismo.

Debe verse como una utilidad premium de sistema.

No debe verse como una herramienta de hacking.

No debe usar estética gamer/neón para acciones normales.

## Dark-first

El tema oscuro es la experiencia principal.

Paleta recomendada:

### Fondo principal

`#08090A`

### Fondo secundario

`#0F1012`

### Superficies

`#121316`
`#16171A`

### Superficie elevada

`#1B1D21`

### Bordes

`rgba(255,255,255,0.06)`
`rgba(255,255,255,0.10)`

En WPF aproximar mediante colores ARGB sólidos o brushes equivalentes.

### Texto principal

`#F5F5F5`

### Texto secundario

`#A1A1AA`

### Texto terciario

`#666970`

### Accent principal

Usar un azul frío o el accent configurado por la app.

El accent no es decoración.

Usarlo para:

- selección;
- progreso;
- acción principal;
- métricas importantes;
- focus;
- estado activo.

## Tema claro

Debe existir porque la app ya soporta tema claro/oscuro.

El tema claro conserva la misma jerarquía.

No crear un tema claro blanco puro y plano.

Usar:

- fondo gris muy claro;
- superficies blancas;
- bordes suaves;
- texto oscuro;
- el mismo sistema semántico.

Todos los colores deben venir de `DynamicResource`.

Nunca hardcodear colores repetidos si pueden vivir en recursos.

---

# 6. TIPOGRAFÍA

Para WPF usar una tipografía moderna disponible en Windows.

Prioridad:

1. Segoe UI Variable;
2. Segoe UI;
3. equivalente del sistema.

Para información técnica:

- Cascadia Mono;
- Consolas si Cascadia no está disponible.

Usar monospace únicamente para:

- logs;
- nombres internos;
- comandos;
- rutas;
- IDs;
- versiones;
- timestamps;
- valores técnicos.

No convertir toda la aplicación en monospace.

## Jerarquía

Mantener pocos tamaños.

Referencia:

- título principal: 24–28;
- título de página: 20–24;
- sección: 16–18;
- cuerpo: 13–14;
- metadata: 11–12;
- métricas: 26–34.

---

# 7. ARQUITECTURA DE LA APP

La arquitectura visual debe responder al producto.

Las áreas actuales se conceptualizan así:

1. **Resumen**
2. **Optimización**
3. **Servicios**
4. **Máquinas virtuales**
5. **Inicio de Windows**
6. **Opciones de WinUtil**
7. **Detalles técnicos**

No es obligatorio convertirlas inmediatamente en páginas separadas.

Si la arquitectura actual usa `TabControl`, preservarla hasta comprobar que una navegación lateral realmente mejora la UX.

Si se implementa navegación lateral:

- compacta;
- sin iconos gigantes;
- agrupada;
- selección activa discreta;
- ancho aproximado 190–230 px;
- opción "Detalles técnicos" visualmente secundaria.

No usar sidebar solo porque sea tendencia.

---

# 8. RESUMEN / HOME

La primera vista debe responder rápidamente:

> ¿Cómo está mi PC y qué puedo hacer?

Mostrar como máximo las métricas principales:

- CPU;
- RAM;
- procesos;
- estado de optimización.

Las métricas deben dominar visualmente.

Ejemplo conceptual:

> 47 %  
> Memoria RAM  
> Normal

No añadir gráficos si no ayudan.

El usuario debe encontrar una acción principal clara:

**Optimizar ahora**

Acciones secundarias:

- Deshacer cambios;
- Crear reporte;
- Ver detalles.

No colocar diez botones con el mismo peso visual.

---

# 9. OPTIMIZACIÓN

Una optimización debe explicar:

- qué se va a cambiar;
- qué no se va a tocar;
- si necesita reinicio;
- si existe backup;
- cómo deshacerlo.

Antes de ejecutar:

> Vamos a pausar servicios que no necesitas todo el tiempo.  
> No modificaremos seguridad, actualizaciones, Wi-Fi, Bluetooth, audio ni touchpad.

Después:

> Optimización completada  
> 3 servicios quedaron disponibles solo cuando se necesiten.

Nunca mostrar únicamente:

> Operation successful.

---

# 10. SERVICIOS

Esta pantalla es crítica.

Debe ser comprensible sin saber qué es un servicio.

## Tabla recomendada

Columnas principales:

- Servicio;
- Estado;
- Cómo inicia;
- Recomendación.

Columnas técnicas:

- nombre interno.

La columna técnica puede tener menor énfasis.

## Estados

Traducir:

- Running → En ejecución;
- Stopped → Detenido;
- Auto → Inicia con Windows;
- Manual → Solo cuando se necesita;
- Disabled → No inicia automáticamente.

## Acciones

Preferir:

- **Iniciar ahora**
- **Detener ahora**
- **Solo cuando se necesite**
- **Iniciar con Windows**
- **No iniciar automáticamente**

Evitar usar solo:

- Start;
- Stop;
- Auto;
- Manual;
- Disabled.

## Servicios protegidos

Deben verse claramente como:

> Protegido  
> Recomendamos no modificar este servicio.

No usar rojo si no existe peligro inmediato.

Un badge neutro o amarillo suave es suficiente.

Los controles incompatibles deben deshabilitarse.

La interfaz nunca debe permitir visualmente algo que el motor bloqueará después si puede evitarse.

---

# 11. MÁQUINAS VIRTUALES

No poner "hypervisorlaunchtype" como título.

La pantalla debe explicar la diferencia funcional entre los dos modos sin ocultar el impacto de seguridad.

## Modo normal

Descripción conceptual:

> Deja disponible el hipervisor de Windows.

Debe explicarse como el modo adecuado para funciones que dependen del hipervisor de Windows, por ejemplo:

- Docker Desktop con WSL2/Hyper-V;
- Hyper-V;
- Windows Sandbox;
- Virtual Machine Platform;
- VBS;
- Integridad de memoria/HVCI cuando esté configurada.

Técnico:

`hypervisorlaunchtype=auto`

## Modo VMware

Descripción conceptual:

> Evita que el hipervisor de Windows arranque en el próximo inicio.

Puede presentarse como una opción de compatibilidad para VMware y virtualización anidada cuando Hyper-V/VBS interfieren.

Técnico:

`hypervisorlaunchtype=off`

### Regla crítica de seguridad

El cambio de modo VMware **no debe borrar ni desactivar permanentemente**:

- políticas de VBS;
- Integridad de memoria;
- Credential Guard;
- configuraciones de Device Guard;
- preferencias de Seguridad de Windows.

La app solo controla el arranque del hipervisor mediante BCD.

Si el hipervisor no está en ejecución, las funciones que dependan de él pueden no estar activas durante esa sesión. Al volver a modo normal, las políticas originales siguen disponibles.

## Estado a mostrar

Mostrar:

- modo configurado;
- hipervisor activo/no activo;
- VBS: no habilitado / configurado no activo / en ejecución;
- Integridad de memoria activa/no activa;
- reinicio pendiente;
- copia de seguridad.

## Perfil de servicios VMware

La preparación de VMware debe diferenciar:

### Principales

- `VMAuthdService`;
- `VMnetDHCP`;
- `VMware NAT Service`.

Al preparar VMware:

- ponerlos en Manual;
- iniciarlos si están instalados.

### USB opcional

- `VMUSBArbService`.

Solo activar cuando el usuario solicite dispositivos USB dentro de las VMs.

### Autoinicio opcional

- `VMwareAutostartService`.

Solo activar cuando el usuario solicite autoinicio de VMs configuradas.

No activar servicios opcionales solo porque existan.

## Acciones

- Usar modo normal;
- Preparar para VMware;
- Pausar servicios VMware;
- Volver al estado original;
- Actualizar estado;
- Reiniciar para aplicar.

El frontend debe dejar claro que:

- cambiar el BCD puede requerir reinicio;
- preparar servicios VMware puede aplicarse inmediatamente;
- no todo cambio requiere reiniciar;
- el estado debe leerse automáticamente antes de sugerir un reinicio.

Los detalles técnicos deben permanecer en tooltips o información secundaria.

---

# 12. INICIO DE WINDOWS

El objetivo es explicar:

> Estas aplicaciones pueden abrirse automáticamente cuando enciendes tu PC.

La vista debe ayudar a distinguir:

- necesario;
- opcional;
- desconocido;
- desactivado;
- activo.

Cuando todavía no exista edición directa, dejar claro:

> Windows11Optimizer muestra la lista y abre la configuración oficial de Windows para realizar cambios.

Nunca hacer creer que un botón modifica algo si solo abre otra pantalla.

---

# 13. WINUTIL

La sección se llama:

**Opciones de WinUtil**

No:

**WinUtil tweaks catalog**

Debe explicar:

> Aquí puedes consultar optimizaciones conocidas de WinUtil explicadas en español.  
> Windows11Optimizer no las aplica automáticamente.

## Tabla

Mostrar:

- Nivel;
- Opción;
- Categoría;
- Perfil;
- Qué modifica;
- Qué hace.

Niveles recomendados:

- Solo consulta;
- Precaución;
- Revisar;
- Avanzado.

No usar "Safe" para una opción que la aplicación aún no ha validado.

## Traducción

Los 66 elementos de la versión fijada deben mostrarse en español.

Conservar internamente:

- ID original;
- título original;
- descripción original;
- categoría original.

No traducir los IDs internos.

No alterar el JSON de WinUtil.

La traducción es solo una capa de presentación.

---

# 14. DETALLES TÉCNICOS

Los logs no deben dominar la experiencia normal.

Ubicarlos en:

**Detalles técnicos**

Usar:

- fuente monospace;
- timestamps;
- buen contraste;
- scroll;
- copy/select;
- agrupación visual.

Evitar cuadros de texto gigantes en la pantalla principal.

---

# 15. CARDS

Usar cards solo cuando agrupen información.

Buenas cards:

- métricas;
- modo de virtualización;
- backup;
- estado del sistema;
- resumen de una acción.

No envolver cada texto en una card.

## Card JR

- borde 1 px sutil;
- radius 8–12;
- padding 16–20;
- sombra mínima o ninguna;
- fondo ligeramente distinto;
- jerarquía tipográfica clara.

No usar glassmorphism fuerte.

No usar cards anidadas sin necesidad.

---

# 16. BOTONES

## Primary

Una acción principal por contexto.

Ejemplos:

- Optimizar ahora;
- Aplicar;
- Guardar.

Alto contraste.

## Secondary

- Deshacer cambios;
- Actualizar;
- Crear reporte.

## Ghost

- Ver detalles;
- Abrir configuración;
- Cancelar.

## Danger

Rojo solo para consecuencias realmente destructivas.

"No iniciar automáticamente" no necesita rojo intenso.

"Reiniciar ahora" necesita warning, no danger extremo.

---

# 17. ICONOGRAFÍA

Usar una sola familia.

Para WPF preferir:

- Segoe Fluent Icons;
- iconografía Fluent compatible;
- otra librería ya existente en el proyecto.

No introducir una dependencia nueva solo para un icono pequeño salvo que aporte valor claro.

Tamaños habituales:

- 16;
- 18;
- 20.

No usar emojis como iconos de interfaz.

Los símbolos simples en toasts pueden utilizarse si son accesibles y consistentes.

---

# 18. ESPACIADO

Escala JR:

- 4;
- 8;
- 12;
- 16;
- 24;
- 32;
- 48;
- 64.

Evitar valores arbitrarios.

Padding habitual:

- controles: 8–12;
- cards: 16;
- secciones: 24.

Mantener densidad media.

La app es técnica, pero no debe sentirse comprimida.

---

# 19. TABLAS

Las tablas deben ser fáciles de escanear.

Requisitos:

- headers visibles;
- alternancia de fila muy discreta;
- selección clara;
- columnas técnicas secundarias;
- truncamiento controlado;
- tooltip si un texto queda cortado;
- no saturar con 8–10 columnas si 4 bastan.

La acción sobre una fila seleccionada debe aparecer cerca de la tabla.

---

# 20. ESTADOS

Toda acción debe contemplar:

- loading;
- éxito;
- advertencia;
- error;
- vacío;
- deshabilitado;
- selección;
- focus;
- hover.

## Loading

Mostrar:

- acción que está ocurriendo;
- indicador visual;
- texto útil.

Ejemplo:

> Actualizando servicios  
> Consultando el estado actual de Windows…

## Success

> Listo  
> VMware quedó configurado para iniciarse solo cuando lo necesites.

## Error

Evitar:

> Error CSxxxx.

Primero:

> No pudimos cambiar este servicio.

Después, opcional:

> Ver detalles técnicos.

---

# 21. TOASTS Y NOTIFICACIONES

Usar notificaciones dentro de la aplicación para feedback corto.

Duración orientativa:

3–5 s.

Tipos:

- información;
- éxito;
- advertencia;
- error.

No mostrar un MessageBox por cada acción exitosa.

MessageBox se reserva para:

- confirmación importante;
- reinicio;
- cambios potencialmente disruptivos;
- errores que requieren intervención.

---

# 22. MICROINTERACCIONES

Animaciones discretas.

Orientación:

- hover: 120–180 ms;
- panel: 180–250 ms;
- transición de vista: 200–300 ms.

En WPF preferir:

- opacity;
- color;
- border;
- transform pequeño;
- progress indeterminado.

No mover grandes paneles sin necesidad.

No usar rebotes.

No usar glow/neón.

---

# 23. ACCESIBILIDAD

Mantener:

- contraste suficiente;
- focus visible;
- navegación con teclado;
- labels claros;
- targets de al menos ~32 px;
- estado no dependiente solo del color;
- texto legible;
- tooltips comprensibles.

Nunca usar:

verde = único indicador de éxito.

Combinar:

- color;
- icono;
- texto.

---

# 24. COPY / MICROCOPY

Escribir para personas.

Preferir:

> Preparar para VMware

sobre:

> Disable Hypervisor

Preferir:

> No iniciar automáticamente

sobre:

> Disabled

Preferir:

> Crear reporte del equipo

sobre:

> Export Diagnostic

Preferir:

> Volver al estado original

sobre:

> Restore BCD Value

Frases:

- cortas;
- directas;
- sin jerga innecesaria;
- consecuencia visible.

---

# 25. SEGURIDAD VISUAL

La UI debe reflejar las protecciones del backend.

Categorías visuales:

## Seguro

Acción ya aprobada por la allowlist.

## Requiere confirmación

Cambio reversible pero con impacto.

## Protegido

No editable por Windows11Optimizer.

## Avanzado

Información o opción externa que no se ejecuta automáticamente.

Nunca presentar como "seguro" algo que solo proviene del catálogo de WinUtil.

---

# 26. INFORMACIÓN PROGRESIVA

Regla:

**simple primero, técnico después**

Nivel 1:

> Preparar para VMware

Nivel 2:

> Desactiva el hipervisor de Windows para el próximo reinicio.

Nivel 3:

> `bcdedit /set {current} hypervisorlaunchtype off`

El usuario normal puede quedarse en nivel 1.

El usuario avanzado puede llegar a nivel 3.

---

# 27. WPF: REGLAS DE IMPLEMENTACIÓN

Preferir:

- `DynamicResource`;
- estilos reutilizables;
- `ResourceDictionary`;
- converters únicamente cuando aporten claridad;
- bindings antes que manipulación visual repetida en code-behind;
- componentes/UserControls cuando una sección crezca demasiado.

Evitar:

- colores hardcoded repetidos;
- margins aleatorios;
- controles duplicados;
- XAML gigantes sin estructura;
- lógica de negocio dentro de XAML;
- lógica visual repetida en múltiples handlers.

No romper handlers existentes durante un rediseño.

Después de modificar XAML comprobar que todos los handlers referenciados existen.

---

# 28. COMPONENTES RECOMENDADOS

Cuando sea útil crear componentes reutilizables:

- AppHeader;
- NavigationItem;
- PageHeader;
- MetricCard;
- StatusBadge;
- ActivityStatusBar;
- Toast;
- SafeActionPanel;
- ServiceStatusRow;
- TechnicalDetailsExpander;
- ConfirmationDialog;
- EmptyState;
- LoadingState;
- WinUtilOptionRow;
- VirtualizationModeCard.

No crearlos todos preventivamente.

Extraer un componente cuando:

- se reutiliza;
- simplifica XAML;
- encapsula comportamiento;
- reduce inconsistencias.

---

# 29. RESPONSIVE DE ESCRITORIO

Aunque sea WPF, debe adaptarse a:

- 1366×768;
- 1920×1080;
- escalado 125 %;
- escalado 150 %;
- ventana reducida.

Evitar:

- ancho fijo excesivo;
- botones cortados;
- texto fuera de pantalla;
- tablas sin espacio;
- scroll horizontal accidental.

Usar:

- Grid;
- `*`;
- `Auto`;
- MinWidth;
- ScrollViewer únicamente donde corresponda;
- WrapPanel para acciones secundarias.

---

# 30. PROHIBICIONES

No usar:

- neón;
- gradientes decorativos constantes;
- glassmorphism exagerado;
- sombras enormes;
- radius de 25–40 px;
- iconos gigantes;
- emojis como sistema de iconos;
- 8 colores de estado diferentes;
- texto diminuto;
- tarjetas para todo;
- barras de progreso falsas;
- datos inventados;
- botones muertos;
- lorem ipsum;
- tecnicismos como copy principal.

No ocultar acciones importantes únicamente en menús contextuales.

---

# 31. REGLA DE SIMPLICIDAD

Cuando dudes entre agregar y quitar:

**quita.**

Cuando dudes entre un color fuerte y uno neutro:

**usa el neutro.**

Cuando dudes entre un término técnico y uno comprensible:

**usa el comprensible y conserva el técnico como detalle.**

Cuando dudes entre cinco acciones visibles y una principal + secundarias:

**prioriza.**

La sofisticación debe provenir de:

- proporción;
- jerarquía;
- tipografía;
- contraste;
- espacio;
- consistencia;
- microinteracción;
- claridad.

No de decoración.

---

# 32. FLUJO DE TRABAJO DE LA SKILL

Cuando se solicite crear o rediseñar una parte del frontend:

## Paso 1 — Analizar

Determina:

- objetivo de la pantalla;
- usuario;
- acción principal;
- riesgos;
- información prioritaria.

## Paso 2 — Inspeccionar

Lee:

- XAML relevante;
- code-behind;
- models;
- servicios usados;
- recursos visuales.

## Paso 3 — Separar

Distingue:

- lógica;
- presentación;
- contenido técnico;
- contenido comprensible;
- estados.

## Paso 4 — Planificar

Define:

- layout;
- jerarquía;
- controles;
- estados;
- copy;
- interacción;
- tema claro/oscuro.

## Paso 5 — Implementar

Modifica el frontend real.

No entregar solamente mockups si el usuario pidió implementación.

## Paso 6 — Verificar

Comprobar:

- compile;
- handlers;
- bindings;
- dark mode;
- light mode;
- 1366×768;
- escalado;
- selección;
- loading;
- error;
- success;
- disabled;
- contraste;
- claridad.

## Paso 7 — Refinar

Eliminar:

- ruido;
- duplicación;
- tecnicismos innecesarios;
- botones redundantes;
- espacios inconsistentes.

---

# 33. CRITERIO DE TERMINACIÓN

El frontend no está terminado solo porque compila.

Debe:

- funcionar;
- ser entendible sin conocimientos avanzados;
- mantener acceso a detalles técnicos;
- respetar tema oscuro y claro;
- ser consistente;
- preservar la lógica;
- mostrar estados correctos;
- explicar acciones importantes;
- sentirse como software final.

No entregar:

- botones sin función;
- navegación rota;
- bindings rotos;
- handlers faltantes;
- textos mezclados español/inglés sin motivo;
- placeholders;
- estilos inconsistentes;
- acciones peligrosas sin explicación.

---

# 34. CHECKLIST JR WIN11

Antes de terminar cualquier cambio visual comprobar:

- [ ] ¿Una persona normal entiende qué hace esta pantalla?
- [ ] ¿La acción principal es obvia?
- [ ] ¿Los términos técnicos están en segundo nivel?
- [ ] ¿Se preservó la lógica existente?
- [ ] ¿Se preservaron eventos y bindings?
- [ ] ¿Funciona en tema oscuro?
- [ ] ¿Funciona en tema claro?
- [ ] ¿Los estados tienen texto además de color?
- [ ] ¿Los servicios protegidos se diferencian?
- [ ] ¿Las acciones avanzadas explican consecuencias?
- [ ] ¿Los mensajes de error son comprensibles?
- [ ] ¿La app sigue siendo usable a 1366×768?
- [ ] ¿Los controles soportan escalado de Windows?
- [ ] ¿No hay cards innecesarias?
- [ ] ¿No hay colores decorativos sin función?
- [ ] ¿No hay términos ingleses visibles sin necesidad?
- [ ] ¿No hay botones muertos?
- [ ] ¿Se siente como una utilidad profesional de Windows?

---

# OBJETIVO FINAL

Windows11Optimizer debe sentirse:

**Dark.  
Clean.  
Technical.  
Precise.  
Minimal.  
Premium.  
Safe.  
Understandable.  
Functional.**

La reacción buscada es:

> "Entiendo qué está haciendo y confío en que puedo deshacerlo."

y también:

> "Esto parece software profesional."

Nunca:

> "No sé qué significa esto."

ni:

> "Parece una plantilla."


---

# 35. MÓDULO OBLIGATORIO — QA VISUAL, DPI Y PREVENCIÓN DE OVERFLOW

Este módulo es **obligatorio** para cualquier modificación de frontend.

Una pantalla que compila puede seguir estando rota visualmente.

Nunca considerar terminado un cambio de UI únicamente porque:

- XAML compile;
- los handlers existan;
- los bindings no den error;
- la aplicación abra.

## 35.1 Matriz mínima de estados

Todo control interactivo modificado o creado debe revisarse en:

- normal;
- hover;
- pressed;
- selected;
- disabled;
- keyboard focus;
- loading cuando corresponda.

Para cada estado verificar:

- texto visible;
- contraste suficiente;
- fondo correcto;
- borde correcto;
- cursor correcto;
- tamaño estable;
- contenido no recortado.

### Regla WPF crítica

Los templates nativos de WPF/Windows pueden ignorar parcialmente:

- Background;
- Foreground;
- BorderBrush;

especialmente en:

- `Button.IsEnabled=False`;
- `TabItem.IsSelected=True`;
- focus;
- pressed;
- controles bajo tema oscuro.

Si un control cambia a blanco, pierde texto o rompe el sistema visual en alguno de esos estados:

**crear o ajustar un `ControlTemplate` propio.**

No intentar resolver únicamente cambiando `Foreground` si el template nativo sigue dibujando otra superficie.

## 35.2 Prueba obligatoria de temas

Probar siempre:

### Dark

- normal;
- selected;
- disabled;
- hover;
- focus.

### Light

- normal;
- selected;
- disabled;
- hover;
- focus.

En dark mode está prohibido que aparezcan accidentalmente:

- botones blancos;
- tabs blancos;
- TextBox blancos no diseñados;
- texto blanco sobre fondo blanco;
- texto oscuro sobre fondo oscuro.

En light mode está prohibido:

- texto demasiado claro;
- bordes invisibles;
- selección indistinguible del fondo.

## 35.3 DPI y escalado de Windows

La UI debe validarse como mínimo para:

- 100 %;
- 125 %;
- 150 %.

Caso crítico obligatorio:

**1366×768 @ 150 %**

El tamaño mínimo de la ventana debe poder entrar razonablemente en el área útil de este escenario.

No fijar un `MinWidth` excesivo solo porque se vea bien a 1920×1080.

Como referencia para esta app:

- preferir `MinWidth` alrededor de 760–820 DIPs;
- usar layouts adaptativos;
- evitar depender de 980+ DIPs mínimos.

Si el contenido necesita más espacio:

**adaptar el layout, no obligar al usuario a tener una pantalla mayor.**

## 35.4 Layout adaptativo

Una fila de 4 módulos no puede mantenerse siempre en 4 columnas.

Usar comportamiento adaptativo.

Referencia:

- ancho amplio → 4 columnas;
- ancho medio/compacto → 2 columnas;
- ancho muy limitado → 1 columna cuando sea necesario.

Aplicar especialmente a:

- métricas;
- tarjetas de estado;
- virtualización;
- grupos de acciones.

Preferir:

- Grid;
- UniformGrid adaptativo;
- WrapPanel;
- `Auto`;
- `*`.

Evitar:

- tamaños absolutos innecesarios;
- columnas rígidas que recorten contenido.

## 35.5 Overflow

Antes de terminar revisar:

- ningún texto sale de su contenedor;
- ningún botón queda cortado;
- ninguna fila de botones desaparece;
- ninguna tabla obliga a scroll horizontal accidental sin motivo;
- los headers de navegación siguen accesibles;
- ningún mensaje largo rompe la ventana.

Textos variables deben considerar:

- traducciones más largas;
- nombres de servicios;
- rutas;
- mensajes de error;
- nombres de aplicaciones.

Usar cuando corresponda:

- `TextWrapping="Wrap"`;
- `TextTrimming`;
- Tooltip con valor completo;
- columnas `*`;
- MinWidth razonable.

## 35.6 Navegación

Para `TabControl`:

- el tab activo debe ser evidente;
- el texto activo debe conservar contraste;
- ningún tab seleccionado puede usar el template blanco por defecto en dark mode;
- headers largos deben seguir siendo accesibles;
- a anchos reducidos se permite que el TabPanel se reorganice.

Si la cantidad de tabs crece hasta romper la navegación:

evaluar navegación lateral o un selector adaptativo.

No ocultar tabs fuera de la ventana.

## 35.7 Botones deshabilitados

Un botón disabled debe seguir explicando qué acción representa.

Debe:

- conservar su texto;
- tener contraste menor pero legible;
- verse claramente inactivo;
- no convertirse en un rectángulo vacío;
- no adoptar el fondo blanco nativo en dark mode.

Referencia visual:

- SurfaceAlt;
- MutedForeground;
- opacity aproximada 0.55–0.70;
- cursor Arrow.

## 35.8 Contenido dinámico

Probar estados reales y estados extremos.

Ejemplos:

- sin backup;
- con backup;
- sin reinicio pendiente;
- con reinicio pendiente;
- servicio Running;
- servicio Stopped;
- servicio protegido;
- lista vacía;
- entrada huérfana;
- error de WinUtil;
- texto largo.

No diseñar únicamente para el estado ideal.

## 35.9 Layout rounding y DPI

En ventanas principales usar cuando sea apropiado:

`UseLayoutRounding="True"`

y:

`SnapsToDevicePixels="True"`

para reducir:

- bordes borrosos;
- líneas de 1 px inconsistentes;
- artefactos a 125/150 %.

## 35.10 Gate de aceptación visual

Antes de declarar terminado un cambio de frontend:

1. Compilar.
2. Verificar handlers.
3. Verificar bindings relevantes.
4. Revisar dark mode.
5. Revisar light mode.
6. Revisar estados disabled.
7. Revisar estados selected.
8. Revisar focus mediante teclado.
9. Revisar ventana compacta.
10. Revisar 1366×768.
11. Considerar 125 %.
12. Considerar 150 %.
13. Revisar textos largos.
14. Revisar overflow.
15. Revisar contraste.
16. Revisar que las acciones sigan siendo comprensibles.

Cuando sea posible ejecutar la app:

**hacer inspección visual real o mediante capturas.**

Cuando el entorno no permita ejecutar/renderizar WPF:

- realizar validación estática;
- no afirmar que la UI fue visualmente verificada;
- pedir o utilizar una captura real en la siguiente iteración;
- tratar cualquier captura del usuario como prueba de regresión.

## 35.11 Regla de regresión por captura

Si el usuario proporciona una captura con un fallo:

1. identificar el control exacto;
2. identificar el estado exacto;
3. buscar la causa raíz en Style/Template/Layout;
4. corregir el sistema reutilizable, no solo un botón;
5. comprobar otros controles que compartan ese Style;
6. añadir la nueva clase de fallo a esta skill si no estaba contemplada.

Una captura de regresión tiene prioridad sobre la suposición de que "debería verse bien".

## 35.12 Fallos que bloquean una entrega

No considerar terminado el frontend si existe cualquiera de estos casos:

- control blanco accidental en dark mode;
- texto invisible;
- botón sin texto;
- tab seleccionado ilegible;
- controles superpuestos;
- controles cortados;
- navegación inaccesible;
- scroll horizontal accidental;
- diálogo fuera de pantalla;
- contenido que requiere una resolución mayor que la declarada;
- disabled indistinguible;
- selección indistinguible;
- focus invisible;
- texto técnico sin explicación en una pantalla para usuario normal.

Estos fallos son **release blockers de UI**.


---

# 36. MÓDULO — OPTIMIZACIÓN INTELIGENTE Y SIMPLIFICACIÓN

Este módulo define la experiencia principal de Windows11Optimizer.

## 36.1 No exponer un administrador genérico de servicios

La aplicación no debe pedir al usuario normal que decida entre:

- Automatic;
- Manual;
- Disabled;
- Start;
- Stop;
- nombres internos de servicios.

Los servicios son implementación interna.

La UI debe hablar de:

- Utilidades Acer;
- Docker;
- VMware;
- componentes en segundo plano;
- disponibles cuando se necesiten.

## 36.2 Política de optimización

La optimización recomendada no debe deshabilitar funciones.

Para componentes explícitamente aprobados por la allowlist:

- preferir inicio Manual;
- mantenerlos disponibles para que una aplicación pueda iniciarlos;
- no forzar el cierre de un componente que ya está funcionando;
- conservar backup del estado anterior;
- permitir restauración.

No usar Disabled como estrategia normal de optimización.

## 36.3 Actividades programadas

No asumir que una tarea programada tiene un equivalente universal a "Manual".

La optimización inteligente debe:

- detectar tareas conocidas;
- explicar su presencia;
- conservarlas si no existe una estrategia específica, reversible y probada;
- no deshabilitarlas automáticamente solo para reducir procesos.

Si en el futuro se administra una tarea, debe hacerse mediante una política específica para esa tarea y con restauración exacta.

## 36.4 Optimización inteligente

El flujo es:

1. analizar;
2. clasificar;
3. mostrar recomendaciones;
4. aplicar solo las aprobadas;
5. volver a analizar;
6. mostrar el resultado;
7. permitir deshacer.

El análisis puede considerar:

- servicios de la allowlist;
- programas de inicio;
- entradas huérfanas;
- Autoruns;
- procesos;
- actividades programadas conocidas;
- estado de virtualización.

Nunca convertir una detección desconocida en un cambio automático.

## 36.5 Máquinas virtuales

La vista normal debe ofrecer únicamente dos decisiones principales:

### Windows y Docker

Prioriza:

- Docker Desktop;
- WSL2;
- Windows Sandbox;
- Hyper-V y funciones equivalentes;
- funciones de seguridad de Windows que dependan de su virtualización.

Los componentes VMware pueden permanecer disponibles bajo demanda, pero no deben forzarse a iniciar con Windows.

### VMware

Prioriza:

- VMware;
- máquinas virtuales;
- compatibilidad con virtualización anidada cuando el modo de virtualización de Windows interfiera.

Al seleccionar VMware:

- preparar internamente los servicios necesarios;
- no pedir al usuario que administre cada servicio;
- conservar los componentes opcionales en modo bajo demanda cuando sea posible.

Información como:

- hypervisorlaunchtype;
- VBS;
- HVCI;
- nombres internos de servicios;

debe permanecer en Información técnica.

## 36.6 Ajustes

La pantalla de ajustes no debe mostrar el catálogo completo de WinUtil.

Mostrar solo opciones:

- portadas nativamente;
- reversibles;
- probadas;
- comprensibles para un usuario normal.

El catálogo completo puede permanecer como fuente interna de referencia, pero no como interfaz de usuario.

## 36.7 Regla de lenguaje

Evitar en la interfaz principal:

- service;
- task;
- trigger;
- registry;
- BCD;
- VBS;
- HVCI;
- automatic/manual/disabled;
- nombres internos.

Preferir:

- disponible cuando se necesite;
- se inicia con Windows;
- actividad programada;
- modo Windows y Docker;
- modo VMware;
- volver al estado anterior.

Los términos técnicos pueden aparecer únicamente en una sección secundaria de información técnica.


### Regla WSL / Docker

El modo **Windows y Docker** debe reparar automáticamente componentes conocidos de WSL y virtualización de Windows si aparecen deshabilitados.

La lógica debe:

- detectar `WslService` y el nombre heredado `LxssManager`;
- detectar `vmcompute` y `hns`;
- detectar componentes instalados relacionados con Sandbox/Hyper-V;
- cambiar únicamente `Disabled -> Manual`;
- conservar cualquier configuración válida existente;
- intentar iniciar WSL/HCS/HNS cuando sea posible;
- aceptar que algunos componentes solo podrán iniciar después del reinicio que aplica el hipervisor normal.

La interfaz no debe mostrar **Listo** únicamente porque `hypervisorlaunchtype=auto`.
También debe considerar si los componentes necesarios quedaron deshabilitados.

Nunca poner estos servicios en Disabled desde la optimización inteligente.


### Contrato inmutable VMwareMode.ps1

El cambio entre **Windows y Docker** y **VMware** conserva como fuente de verdad el comportamiento del script original `VMwareMode.ps1`.

Regla exacta:

- si `bcdedit /enum {current}` contiene `hypervisorlaunchtype off` -> modo VMware;
- cualquier otro caso -> modo Windows/Normal;
- Windows/Normal escribe únicamente `hypervisorlaunchtype auto`;
- VMware escribe únicamente `hypervisorlaunchtype off`.

Este núcleo no administra:

- servicios;
- tareas programadas;
- WSL;
- Docker;
- VMware;
- VBS;
- Integridad de memoria.

## Orden obligatorio

Al cambiar de modo:

1. aplicar primero el valor BCD `auto/off`;
2. guardar el estado de reinicio;
3. después ejecutar preparación secundaria;
4. comprobar la preparación;
5. informar por separado:
   - modo base aplicado;
   - preparación secundaria completa/incompleta.

Un error al preparar servicios no puede:

- impedir que se guarde el cambio BCD;
- revertir el cambio BCD;
- hacer que la UI diga que el cambio BCD falló si realmente se aplicó.

## Windows y Docker

Después de aplicar `auto`:

- reparar únicamente servicios conocidos que estén en Disabled;
- conservar cualquier modo válido existente;
- no deshabilitar servicios de WSL/virtualización;
- permitir que el reinicio termine de activar el hipervisor si hace falta.

## VMware

Después de aplicar `off`:

- dejar componentes VMware disponibles;
- preparar los componentes principales;
- no modificar políticas de seguridad de Windows;
- no mezclar la preparación VMware con la decisión BCD.

Esta separación es una regla de arquitectura, no solo una decisión visual.


---

# 37. MÓDULO — IA SEGURA, APLICACIONES Y FOCUS BOOST

## 37.1 Zona protegida

Los módulos nuevos no deben modificar:

- `VirtualizationModeCore`;
- la semántica `hypervisorlaunchtype auto/off`;
- la preparación Docker/WSL;
- la preparación VMware.

Docker ↔ VMware es una zona protegida.

## 37.2 Examen con IA

La IA:

- analiza;
- explica;
- recomienda.

La IA nunca:

- ejecuta;
- construye comandos que se ejecuten;
- modifica Registro;
- modifica servicios;
- elimina aplicaciones;
- elimina archivos;
- cambia virtualización.

La respuesta de IA debe limitarse a IDs del catálogo local.

El backend debe volver a validar cada ID antes de mostrarlo y antes de permitir Aplicar.

## 37.3 Catálogo seguro

Cada acción visible debe declarar:

- título comprensible;
- qué hace;
- impacto;
- riesgo;
- cómo se deshace;
- si puede aplicarse automáticamente;
- si puede ser seleccionada por IA.

No incluir acciones Docker/VMware en el catálogo de IA.

## 37.4 Aplicaciones

Desinstalar significa:

- iniciar el desinstalador registrado por Windows.

No significa:

- borrar una carpeta;
- eliminar claves heurísticamente;
- ejecutar comandos inventados por IA.

Los residuos automáticos deben estar confirmados por una regla local específica y reversible.

## 37.5 Focus Boost

Focus Boost es temporal.

Reglas:

- target elegido explícitamente;
- bloquear procesos críticos;
- no usar High;
- no usar Realtime;
- guardar prioridad anterior;
- restaurar al terminar el proceso;
- persistir sesión para recuperación;
- cambios en procesos secundarios solo mediante allowlist.

Preferir reducir prioridad a suspender procesos.

## 37.6 Agente

El agente debe:

- ser un EXE separado;
- ejecutarse `asInvoker`;
- no cargar WPF principal al iniciar Windows;
- usar bandeja;
- registrar hotkeys globales;
- detectar conflictos de hotkeys;
- permitir desactivar autoinicio;
- lanzar la UI principal solo bajo petición.

Hotkeys predeterminados:

- Ctrl+Alt+Space → Mini Focus Boost;
- Ctrl+Alt+O → UI completa.

## 37.7 Privacidad IA

Enviar solo los datos necesarios.

No enviar por defecto:

- claves;
- tokens;
- contenido de documentos;
- líneas de comando completas;
- rutas personales completas;
- títulos de ventanas si no son necesarios.

La clave API debe venir de una fuente externa segura; nunca hardcodearla.
