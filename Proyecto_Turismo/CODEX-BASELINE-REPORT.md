# CODEX BASELINE REPORT — WhyPath

Fecha: 2026-10-09 (America/Lima)
Alcance: auditoría reproducible del estado actual, sin rediseño, reescritura ni correcciones.

## Entorno

- Windows 11 Pro `10.0.26300` build `26300`, 64 bits.
- .NET SDK `10.0.301`; host/runtime ASP.NET Core `10.0.9` x64.
- `backend/global.json` solicita `10.0.301`; está disponible.
- Node.js `v22.23.1`; npm `10.9.8`.
- No se encontraron `AGENTS.md` ni documentos bajo `docs/`.
- Raíz Git: `C:\Users\Admin\Documentos\Proyecto_Turismo`; frontend en `Proyecto_Turismo/`, backend en `backend/`.

Evidencia: `Get-CimInstance Win32_OperatingSystem`, `dotnet --info`, `dotnet --list-sdks`, `node --version`, `npm --version`, `git rev-parse --show-toplevel` y búsqueda recursiva de instrucciones.

## Git

- Estado inicial limpio: `## main...origin/main`.
- Rama: `main`.
- Último commit: `6c391da9652647e56677ea497a1315ff61d5bbb1`, `2026-10-03T19:10:06-05:00`, asunto `trigger netlify deploy`.
- Ramas: local `main`; remotas `origin/main` y `origin/HEAD -> origin/main`.
- Ignorados importantes: `backend/**/bin/`, `backend/**/obj/`, `backend/npxskillui/node_modules/`.
- Después de restore/build/test, Git seguía limpio; los artefactos quedaron en rutas ignoradas.

Evidencia: `git status --short --branch`, `git branch --show-current`, `git log -1`, `git branch -a -vv`, `git status --ignored --short`.

## Build

- Solución: `backend/WayPath.slnx`, cinco proyectos, target `net10.0`.
- Restore correcto: un proyecto restaurado y cuatro actualizados.
- Build Debug aislado correcto: 0 warnings, 0 errores, `00:00:14.58`.
- Comando: `dotnet build WayPath.slnx --configuration Debug --no-restore --verbosity minimal` desde `backend/`.
- Una primera ejecución solapó build/test y causó bloqueos transitorios de DLL por `testhost`; se descartó. La ejecución aislada posterior es la baseline válida.

## Tests

- Proyecto: `backend/tests/WayPath.Tests/WayPath.Tests.csproj`.
- Comando: `dotnet test tests\WayPath.Tests\WayPath.Tests.csproj --configuration Debug --no-build --logger "console;verbosity=minimal"`.
- Resultado aislado: total 27, passed 27, failed 0, skipped 0; duración 2 m 4 s.
- No se modificaron tests.

## Configuración

Nombres obligatorios/observados, sin valores:

- `ConnectionStrings:WayPathDB`
- `Jwt:SigningKey`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:AccessTokenMinutes` / `Jwt:LifetimeMinutes`
- `AllowedHosts`

En Development todos se resolvieron como presentes/no vacíos. La aplicación valida clave Base64 de al menos 32 bytes, issuer/audience, vigencia y la cadena `WayPathDB` (`Program.cs:19-28`).

Servicio del frontend:

- Development: `Program.cs:165-173` sirve `../../../Proyecto_Turismo` con `PhysicalFileProvider`, `UseDefaultFiles` y `UseStaticFiles`.
- Publish: `WayPath.Api.csproj:9` copia `../../../Proyecto_Turismo/**/*` a `wwwroot/**` con `CopyToPublishDirectory="PreserveNewest"`; fuera de Development se usa el web root publicado.

## DB

- Instancia LocalDB detectada: `MSSQLLocalDB`.
- `WayPathDB` es accesible para la aplicación: `/health/ready` devolvió 200 y `GET /api/catalogo/destinos` devolvió 200 con contenido, ejercitando el catálogo real.
- Versión exacta y enumeración real de schemas/tablas: **BLOQUEADO**. `sqlcmd.exe` no está instalado y `Microsoft.Data.SqlClient` de .NET 10 no cargó dentro de Windows PowerShell. No se simuló el resultado.
- Modelo EF: 67 mapeos `ToTable` en schemas `Auditoria`, `Catalogo`, `Cliente`, `Configuracion`, `Operacion`, `Pagos`, `Reportes`, `Seguridad` y `Ventas`.
- Comparación EF vs. DB: **BLOQUEADA** por no poder consultar `sys.schemas/sys.tables` con las herramientas disponibles.
- No hay directorio `Migrations`, archivos `.sql`, `Database.Migrate`, `EnsureCreated` ni `EnsureDeleted`. Los mapeos usan `ExcludeFromMigrations()`; no se encontró mecanismo de creación/migración/seed estructural dentro del repo.
- No se alteró la DB.

## API/health

- API Development: `dotnet run --no-build --project src\WayPath.Api\WayPath.Api.csproj --urls http://127.0.0.1:5187`.
- Inicio correcto; content root `backend/src/WayPath.Api`.
- `/health/live`: 200 JSON.
- `/health/ready`: 200 JSON.
- `/openapi/v1.json`: 200; 103 paths y 124 operaciones HTTP reales.
- `/`: 200, frontend servido.
- No se observaron fallos de rutas al iniciar. OpenAPI solo se mapea en Development (`Program.cs:186`).
- Auth anónima: `/api/auth/me` devuelve 401, esperado.

## Frontend/assets

- Home, Destinos, Login, Registro, Reserva y Admin respondieron y generaron capturas en los tres viewports.
- Evidencia temporal: `%TEMP%\waypath-baseline-20261009\` (18 PNG).
- El navegador integrado falló al iniciar dos veces; Playwright no está instalado. Consola DevTools y waterfall/network completo: **BLOQUEADOS**.
- No existe cuenta de prueba real documentada; solo fakes en `backend/tests`. No se reutilizaron contra datos reales.
- Navegación anónima: públicas cargan; Reserva pide sesión; Admin muestra “Acceso restringido”.
- Login cliente/admin: **BLOQUEADO** por falta de credenciales de prueba documentadas. No se crearon cuentas.
- Checkout confirmado como simulación: `Proyecto_Turismo/js/pago.js:75-82` envía `resultadoSimulado` a `/api/pagos/intentos` y lo etiqueta como simulado. No se ejecutó pago.

Asset case-sensitive:

- `css/login.css:28` referencia `../img/machupichu-login.jpg`.
- Git versiona `Img/machupichu-login.jpg`, no un directorio `img/` separado.
- Funciona en Windows case-insensitive; puede dar 404 en Linux/case-sensitive. Severidad Media.

## Seguridad observada

- `appsettings.Development.json` versiona una `Jwt:SigningKey` no vacía. El valor se omite. Una clave de firma trackeada es reutilizable si ese perfil se expone. Severidad Alta.
- La configuración Development de LocalDB también está versionada; no se reproduce su contenido.
- JWT por cookie `HttpOnly`, `SameSite=Strict`; `Secure` fuera de Development (`JwtSession.cs:18,30-38`).
- Antiforgery con cookie `HttpOnly`, `SameSite=Strict`, `Secure=Always` fuera de Development (`Program.cs:64-71`).
- Rate limiting en login, registro y cambio de contraseña (`AuthController.cs:19,26,65`; `Program.cs:123-134`).
- Producción usa HSTS y redirección HTTPS (`Program.cs:151-163`).
- Middleware: `Referrer-Policy: strict-origin-when-cross-origin`, CSP `frame-ancestors 'none'` y `Cache-Control: no-store` para `/api` (`Program.cs:145-148`).

## Responsive

Evidencia: Chrome headless, `390x844`, `768x1024` y `1440x900`.

- Home: desktop correcto; a 390 px hero, CTA y controles exceden/recortan el ancho.
- Destinos: tablet usa dos columnas; a 390 px título, texto y chips se recortan y hay overflow aparente.
- Login: desktop/tablet renderizan; a 390 px el formulario queda cortado a la derecha.
- Registro: desktop centrado; a 390 px la tarjeta supera el viewport.
- Reserva: a 390 px encabezado, tabs, tarjeta y campos exceden el ancho; el estado anónimo sí aparece.
- Admin: desktop muestra sidebar y acceso restringido; a 390 px adopta header compacto, pero conserva recorte lateral.
- Controles flotantes de idioma/accesibilidad se superponen al contenido inferior en varios tamaños.

No se corrigió diseño.

## Performance

- Lighthouse: **NO VIABLE**; no está instalado y no se instalaron dependencias.
- Playwright ausente; navegador integrado falló al iniciar.
- LCP/CLS/INP: no disponibles con evidencia fiable. No se inventan métricas ni scores.

## Bloqueadores

1. Schemas/tablas y versión SQL exacta: falta cliente SQL utilizable.
2. Login cliente/admin: no hay credenciales reales de prueba documentadas.
3. Consola/network completos: Playwright ausente y navegador integrado no inicializa.
4. Lighthouse/Web Vitals: tooling ausente.
5. Decisiones operativas: no existen `AGENTS.md` ni `docs/**`.

## Hallazgos por severidad

| Severidad | Hallazgo | Evidencia |
|---|---|---|
| Alta | Clave JWT Development versionada | `backend/src/WayPath.Api/appsettings.Development.json`; valor omitido |
| Alta | Sin creación/migración de esquema dentro del repo | Sin `Migrations/`, `.sql`, `Migrate/EnsureCreated`; `ExcludeFromMigrations()` |
| Media | Overflow/recorte horizontal móvil | Capturas 390x844 de las seis páginas |
| Media | Asset con case mismatch `img` vs `Img` | `css/login.css:28` vs archivo versionado |
| Media | Comparación DB incompleta | Ready y catálogo OK, pero sin enumeración SQL read-only |
| Media | Sin documentación operativa | Ausencia de `AGENTS.md` y `docs/**` |
| Baja | Controles flotantes superpuestos | Capturas responsive |
| Baja | Tooling web no reproducible desde repo | Playwright/Lighthouse ausentes |

No se asigna severidad Crítica: no se confirmó explotación activa, pérdida de datos, bypass de autorización ni indisponibilidad productiva.

## NO CAMBIÉ

- No alteré código funcional ni tests.
- No alteré esquema, contenido ni configuración de DB; no borré, migré ni sembré datos.
- No imprimí secretos en este reporte; solo nombres y estados de resolución.
- No ejecuté pagos reales ni simulados contra datos reales.
- No hice deploy, commit, push, merge, rebase ni cambios de rama.
- No corregí UI, arquitectura, configuración ni hallazgos.
- El único archivo nuevo deliberado es `Proyecto_Turismo/CODEX-BASELINE-REPORT.md`; restore/build/test generaron solo artefactos ignorados en `bin/` y `obj/`.
