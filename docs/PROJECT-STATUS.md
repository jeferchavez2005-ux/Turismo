# WhyPath — Project Status

Última actualización: 24 de octubre de 2026.

## Arquitectura

- Frontend estático en `Proyecto_Turismo/`: páginas HTML, estilos CSS, JavaScript y recursos locales. La navegación y el footer públicos se generan desde `Proyecto_Turismo/js/navbar.js`.
- Backend ASP.NET Core en `backend/`. Sus contratos API y reglas de negocio no se modifican desde las tareas de interfaz.
- SQL Server `WayPathDB` queda fuera del trabajo UX/UI. No ejecutar `BD.sql`, migraciones ni comandos SQL durante estas fases.

## Estado de WhyPath V2

- Bloque visual principal terminado para Home, Destinos, autenticación y Reserva; Pago conserva su lógica y recibe únicamente estilos globales compartidos.
- Controles globales de idioma, tema y accesibilidad adaptados para evitar superposiciones en pantallas pequeñas.
- Navegación pública y footer unificados; Contacto permanece en el menú generado. Los enlaces privados y administrativos conservan las comprobaciones de sesión y rol existentes.
- Páginas secundarias refinadas: Contacto, Compras, Cuenta y Reseñas comparten ahora la densidad, superficies, radios, espaciado y comportamiento responsive del sistema V2. No se cambiaron sus llamadas API ni su JavaScript funcional.
- Panel administrativo refinado para WhyPath V2: shell, navegación responsive, dashboard, tablas adaptativas, filtros, formularios, tarjetas y estados comparten ahora el sistema visual global sin cambiar permisos, APIs ni reglas de negocio.
- Bloqueos frontend de Reserva y Mi cuenta corregidos: la reserva carga perfil y pasajeros aunque aún no exista un destino seleccionado, Cuenta ofrece navegación funcional entre módulos y la navbar pública ya no muestra el acceso directo «Reservas».
- Traducciones ampliadas y sincronizadas en ES, EN, FR y PT para login, registro, reserva y las secciones principales de Cuenta; Home y Destinos conservaron sus claves completas existentes.

## Últimos commits confirmados

- `1596d4a95e5e4927f4aaa397e978f06618e12cd4` — `feat(nav): unify public navigation and footer`
- `dc375baaa39649052824763b7382e17c051f82e7` — `feat(ui): implement WhyPath V2 visual improvements`

## Decisiones y restricciones permanentes

- Mantener visible la marca WayPath y la paleta vigente hasta que exista una decisión formal de branding.
- Reutilizar el sistema visual global; no incorporar frameworks ni dependencias nuevas.
- Preservar autenticación, reservas, pagos, autorización administrativa, contratos API y reglas de negocio.
- No modificar backend ni SQL Server desde tareas de interfaz, y no incluir `BD.sql` en commits UX/UI.
- No sobrescribir cambios del usuario ni modificar los reportes de auditoría salvo que una tarea lo solicite expresamente.
- No desplegar ni usar operaciones Git destructivas o `push --force`.

## Pruebas y limitaciones

- Las fases V2 anteriores se comprobaron con servidor estático en 1440 × 900 y 390 × 844, temas claro y oscuro, incluyendo navegación anónima, overflow horizontal y recursos locales.
- Para este bloque se verificaron el servidor estático, el diff y las reglas responsive. Se obtuvo una captura móvil real de Contacto que confirmó el reflujo de tarjetas.
- La ejecución completa de capturas de este bloque quedó limitada por un cierre de Chrome headless causado por el proceso GPU del entorno; las combinaciones no generadas no se consideran probadas visualmente.
- No hay credenciales de prueba ni una sesión autenticada real disponible. Los estados de Cuenta, Compras y creación de Reseñas que dependen del backend siguen pendientes de validación integrada.
- La fase administrativa se revisó a nivel estático y responsive; Chrome headless sólo confirmó visualmente el tema oscuro y el estado de acceso restringido. El tema claro, los datos reales y las operaciones autenticadas continúan pendientes por falta de una sesión administrativa de prueba y por limitaciones del navegador del entorno.
- Las correcciones funcionales se comprobaron con API vacía controlada, sin consultar `WayPathDB`, en 2560×1440, 1920×1080, 1366×768 y 390×844: sin errores JavaScript ni overflow horizontal. No se creó ni confirmó ninguna reserva.
- El mensaje «No se pudo completar la operación» corresponde al `POST /api/reservas` con HTTP 500 y una excepción no clasificada por `ApiExceptionHandler`. El cliente registra ahora ruta, estado y `traceId`; determinar la excepción interna sigue pendiente de logs de una sesión real y acceso autorizado a la infraestructura, sin consultar ni modificar SQL desde esta fase.

## Siguiente tarea prevista

Reproducir el `POST /api/reservas` con una sesión de cliente autorizada y correlacionar su `traceId` con logs de aplicación; la causa de infraestructura/datos no se considera resuelta sin esa evidencia. Completar también la regresión administrativa autenticada.
