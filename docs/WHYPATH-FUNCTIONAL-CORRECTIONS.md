# Correcciones funcionales integrales de WayPath

Fecha de ejecución: 12 de noviembre de 2026

## Resumen ejecutivo

Se corrigieron causas reproducibles en el flujo de reserva, filtros de reseñas, conservación visual de la sesión, CORS de desarrollo y escalado tipográfico. Las correcciones se integraron con `api.js`, cookies HTTP-only, CSRF y el sistema de traducciones existentes. No se ejecutó la API, no se abrió ninguna conexión SQL y no se accedió ni modificó `WayPathDB`.

La recuperación de contraseña no puede completarse de forma segura con el esquema y los servicios actuales: no existe almacenamiento de tokens de recuperación ni proveedor de correo. Crear la interfaz sin esas dependencias produciría un flujo engañoso. Este punto queda bloqueado por dependencias externas y requerirá una decisión de arquitectura y un cambio de esquema administrado fuera de esta ejecución.

## Reserva y pasajeros

### Causa raíz

`Proyecto_Turismo/js/reserva.js` ejecutaba contexto, perfil y pasajeros dentro de un único bloque secuencial. Un error al cargar el perfil interrumpía la secuencia antes de `loadPassengers`, dejando permanentemente el texto “Cargando pasajeros...”. La selección de viajeros y los datos editados tampoco se persistían antes de navegar a Mi cuenta.

`Proyecto_Turismo/js/cuenta.js` ignoraba el `returnUrl` seguro enviado por reserva. El enlace “Ir a reservar” siempre usaba una ruta fija.

### Corrección

- La autenticación sigue siendo el requisito inicial, pero contexto de viaje, perfil y pasajeros se cargan de forma independiente.
- Un fallo de perfil ya no impide recuperar pasajeros.
- La selección, categorías, titular y campos editables se guardan en `sessionStorage`, nunca en la URL.
- El borrador se restaura al volver desde Mi cuenta y se elimina después de crear correctamente la reserva.
- Mi cuenta valida `returnUrl` como ruta interna antes de aplicarlo al enlace de regreso.
- El backend sigue calculando y validando tarifa, cupos, pasajeros y total.

Estado: **CORREGIDO, PENDIENTE DE VERIFICACIÓN REAL**. La sintaxis fue verificada, pero no fue posible ejecutar el backend por ausencia del SDK requerido.

## Reseñas y filtros

### Causa raíz

El endpoint ya admitía `destinoId` y `calificacion`, pero el frontend descargaba todas las reseñas y filtraba por el nombre exacto del destino. El DTO público no exponía `DestinoId`, aunque la entidad sí lo tenía. Respuestas lentas podían reemplazar resultados de un filtro más reciente.

La regla de elegibilidad sigue exigiendo una reserva `COMPLETADA`; el pago aprobado solo deja la reserva `CONFIRMADA`. Esta separación es correcta y se conservó.

### Corrección

- `ResenaDto` incluye `DestinoId` sin cambiar tablas.
- El selector se llena desde el catálogo con IDs estables.
- Destino y estrellas se envían al endpoint y funcionan solos o combinados.
- “Todas las calificaciones” omite el parámetro de estrellas.
- Un contador de solicitud descarta respuestas tardías.
- Estados de carga, vacío, red, autenticación, permisos, elegibilidad y error interno tienen mensajes diferenciados y `traceId` cuando existe.
- Los textos dinámicos nuevos se añadieron a ES, EN, FR y PT.

Estado: **CORREGIDO, PENDIENTE DE VERIFICACIÓN REAL**.

## Internacionalización

Se mantuvo el selector existente y su persistencia mediante `localStorage`. Se añadieron traducciones para todos los estados dinámicos nuevos de reseñas y se cambió el formateo de fechas para usar el idioma activo.

La auditoría confirma que continúan existiendo textos históricos escritos directamente en HTML y JavaScript en distintas páginas. La cobertura total de cada página no quedó completada en esta ejecución.

Estado: **PARCIALMENTE CORREGIDO**.

## Sesión y CORS

### Causa raíz

`Proyecto_Turismo/js/navbar.js` capturaba cualquier error de `/api/auth/me` y devolvía `null`. Por ello, un error de red o HTTP 500 reemplazaba una sesión visualmente válida por navegación anónima.

En desarrollo, el frontend estático usa `127.0.0.1:5500` o `localhost:5500`, mientras la API usa `localhost:5187`; no existía middleware CORS.

### Corrección

- Solo HTTP 401 confirma el estado anónimo.
- Errores de red, 403 o 500 conservan el último estado conocido y no disparan cierre de sesión.
- La URL predeterminada de la API evita mezclar innecesariamente `127.0.0.1` y `localhost`.
- CORS se habilita únicamente cuando existe una lista configurada, con orígenes explícitos, métodos y encabezados requeridos y credenciales. No se usa `AllowAnyOrigin`.

Estado: **CORREGIDO, PENDIENTE DE VERIFICACIÓN REAL**.

## Recuperación de contraseña

No existen entidad o tabla de token de recuperación, proveedor de correo ni configuración de remitente. El almacenamiento de un token seguro, con hash, expiración, uso único e invalidación requiere persistencia adicional. La prohibición de modificar SQL Server impide incorporar esa capacidad en esta ejecución.

Dependencias requeridas:

- almacenamiento persistente de hash de token, usuario, expiración, consumo e intentos;
- proveedor transaccional de correo y configuración segura fuera del repositorio;
- plantilla y URL pública HTTPS;
- rate limiting específico y pruebas con proveedor falso.

Estado: **BLOQUEADO POR DEPENDENCIA EXTERNA**.

## Tamaño de texto

### Causa raíz

El control modificaba el tamaño del `body`, pero la mayoría de componentes usa `rem`, que se calcula desde `html`. Esos componentes no cambiaban de tamaño.

### Corrección

La escala se aplica a `html` mediante `--font-scale`; el cuerpo hereda `1rem`. Los límites existentes son 94 %, 100 % y 112 %, la preferencia permanece en `localStorage` y no se aplica una transformación global a imágenes.

Estado: **CORREGIDO, PENDIENTE DE VERIFICACIÓN REAL**.

## Manejo de errores

Se preservaron los cambios previos no confirmados de `ApiExceptionHandler`, que agregan diagnóstico saneado sin exponer tokens, contraseñas, correos o cadenas de conexión. Los errores nuevos del módulo de reseñas distinguen red, 401, 403, 409 y error interno, y muestran `traceId` cuando el backend lo proporciona.

Estado: **PARCIALMENTE CORREGIDO**; otros módulos todavía contienen mensajes directos no centralizados.

## Archivos modificados por esta ejecución

- `Proyecto_Turismo/js/reserva.js`: carga independiente y borrador de reserva.
- `Proyecto_Turismo/js/cuenta.js`: retorno seguro a la reserva.
- `Proyecto_Turismo/cuenta.html`: enlace de regreso traducible y compatible con retorno.
- `Proyecto_Turismo/js/reseñas.js`: filtros por API, control de carreras, elegibilidad y errores.
- `Proyecto_Turismo/js/navbar.js`: estado de autenticación resistente a errores temporales.
- `Proyecto_Turismo/js/api.js`: origen de desarrollo coherente.
- `Proyecto_Turismo/css/shared.css`: escalado tipográfico desde la raíz.
- `Proyecto_Turismo/locales/es.json`, `en.json`, `fr.json`, `pt.json`: mensajes dinámicos nuevos.
- `backend/src/WayPath.Application/VentaContracts.cs`: ID estable de destino en reseñas.
- `backend/src/WayPath.Infrastructure/VentaStore.cs`: proyección del ID del destino.
- `backend/src/WayPath.Api/Program.cs`: política CORS explícita y condicional.
- `backend/src/WayPath.Api/appsettings.Development.json`: orígenes locales permitidos.
- `docs/WHYPATH-FUNCTIONAL-CORRECTIONS.md`: este informe.

Se conservaron sin sobrescribir los cambios preexistentes en `ApiExceptionHandler.cs`, `docs/PROJECT-STATUS.md` y las pruebas no rastreadas.

## Pruebas y evidencias

### Ejecutadas

- `dotnet restore WayPath.slnx`: no ejecutó restauración; falta SDK `10.0.301` y solo está instalado `11.0.100`.
- Validación sintáctica con `node --check`: correcta para reserva, reseñas, navbar y cuenta.
- Parseo JSON: correcto para ES, EN, FR y PT.
- `git diff --check`: sin errores de espacios; solo avisos de normalización LF/CRLF.
- Detector de interfaz: encontró advertencias históricas de tipografía, contraste y sombras; no se cambió la identidad visual porque queda fuera del alcance funcional.

### No ejecutadas

- `dotnet build` y `dotnet test`: bloqueadas por el SDK ausente.
- Flujo real con autenticación, cookies y API: no se levantó el backend para evitar cualquier posibilidad de conexión a `WayPathDB`.
- Envío de correo: no existe proveedor.
- Verificación visual completa en 1440 × 900, 768 × 1024 y 390 × 844: pendiente.

## Estado final

| Área | Estado |
|---|---|
| Reserva y pasajeros | CORREGIDO, PENDIENTE DE VERIFICACIÓN REAL |
| Filtros de reseñas | CORREGIDO, PENDIENTE DE VERIFICACIÓN REAL |
| Publicación y elegibilidad de reseñas | CORREGIDO, PENDIENTE DE VERIFICACIÓN REAL |
| Internacionalización completa | PARCIALMENTE CORREGIDO |
| Cierre inesperado de sesión | CORREGIDO, PENDIENTE DE VERIFICACIÓN REAL |
| CORS local | CORREGIDO, PENDIENTE DE VERIFICACIÓN REAL |
| Recuperación de contraseña | BLOQUEADO POR DEPENDENCIA EXTERNA |
| Tamaño de texto | CORREGIDO, PENDIENTE DE VERIFICACIÓN REAL |
| Manejo general de errores | PARCIALMENTE CORREGIDO |

## Confirmación de protección de datos

No se ejecutó `BD.sql`, no se ejecutaron migraciones, no se creó esquema, no se consultó SQL Server, no se inició la API y no se accedió ni modificó `WayPathDB`.
