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

Usar:

## Modo normal

Descripción:

> Compatible con las funciones de virtualización de Windows.

## Modo VMware

Descripción:

> Puede ayudar cuando VMware tiene conflictos con el hipervisor de Windows.

Mostrar:

- modo actual;
- modo preparado para el próximo reinicio;
- si existe copia de seguridad;
- reinicio pendiente.

Acciones:

- Usar modo normal;
- Preparar para VMware;
- Volver al estado original;
- Reiniciar ahora.

El detalle:

`hypervisorlaunchtype=auto/off`

debe ir en tooltip o sección técnica.

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
