# WayPath: arquitectura y base de Seguridad

Fecha: 2026-09-09. Alcance implementado: Seguridad, no los modulos comerciales.

## 1. Estado del proyecto y estrategia

El repositorio original contiene una maqueta multipagina HTML/CSS/JavaScript, sin un backend previo.
Se revisaron las 14 paginas originales: index, Destinos, servicios, contacto, login, registro,
reserva, compras, pago, resenas, los dashboards de ventas y marketing, y las dos landing pages.

El acceso original se simulaba guardando un nombre en localStorage. Los scripts de autenticacion
antiguos tambien contenian almacenamiento de contrasenas en texto plano y referencias a controles
que no coincidian con los formularios cargados. No se importan esas credenciales como cuentas reales.

Se conserva la interfaz existente. La sustitucion progresiva comienza por identidad:
- Login y registro ahora llaman a la API; ya no autentican mediante localStorage.
- La barra de navegacion consulta la sesion real.
- Se agrega cuenta.html para nombre de perfil, cambio de contrasena y cierre de sesion.
- Reservas, pagos, catalogo, compras, resenas, contacto y dashboards siguen siendo maqueta.
- Los datos locales de destinos y preferencias de accesibilidad se conservan.
- Solo se retiran las claves obsoletas waypath_usuario, db_password, db_correo y db_nombre.

La existencia de un dashboard HTML no implica que sus datos o acciones sean reales.
Tampoco se restringe el acceso a esas paginas de demostracion como si fueran administracion operativa.

## 2. Arquitectura implementada

Monolito modular con cuatro proyectos. No se introducen microservicios ni se exige migrar a React.

```text
Navegador: HTML + CSS + JavaScript existente
        | fetch /api/*, mismo origen, cookie HttpOnly + cabecera CSRF
        v
WayPath.Api: controllers, JWT, politicas RBAC, validacion y HTTP
        |
WayPath.Application: contratos, casos de uso de autenticacion, interfaces
        |
WayPath.Infrastructure: implementacion del acceso, PasswordHasher, EF Core
        |
WayPath.Domain: entidades que representan tablas existentes
        |
SQL Server: WayPathDB (sin modificaciones de esquema)
```

Las dependencias de codigo apuntan API -> Infrastructure -> Application -> Domain.
Application depende de interfaces; Infrastructure las implementa y API configura la inyeccion.
ISecurityStore es un acceso especifico del modulo, no un repositorio generico por cada tabla.
Las comprobaciones que necesitan consistencia entre varias filas se ejecutan dentro de transacciones.

```text
backend/
  WayPath.slnx
  global.json
  Directory.Build.props
  NuGet.Config
  src/
    WayPath.Domain/
      Seguridad/
      Configuracion/
    WayPath.Application/
      SecurityContracts.cs
      AuthenticationService.cs
    WayPath.Infrastructure/
      WayPathDbContext.cs
      SecurityStore.cs
      PasswordService.cs
    WayPath.Api/
      Controllers/
      Security/
      Properties/launchSettings.json
      Program.cs
      BootstrapAdmin.cs
      appsettings.json
      appsettings.Development.json
  tests/WayPath.Tests/
Proyecto_Turismo/
  js/api.js
  js/auth-state.js
  js/login.js
  js/registro.js
  js/cuenta.js
  cuenta.html
  ...frontend conservado
```

Se usa .NET 10, ASP.NET Core, EF Core SQL Server 10.0.12, JwtBearer 10.0.12,
OpenAPI y xUnit. global.json selecciona el SDK 10.0.301 instalado.
La configuracion local conserva SQL Server como fuente de verdad; no hay base paralela JSON.

## 3. Tablas existentes utilizadas

Los nombres fisicos reales estan en plural, aunque las clases C# son singulares.

| Clase | Tabla real | Uso |
|---|---|---|
| Usuario | Seguridad.Usuarios | Email, hash, nombre, estado, fechas, rowversion |
| Rol | Seguridad.Roles | Codigo y nombre de rol; solo roles activos autorizan |
| Permiso | Seguridad.Permisos | Codigo estable, modulo y accion |
| UsuarioRol | Seguridad.UsuarioRoles | Relacion N:M usuario-rol, fecha y asignador |
| RolPermiso | Seguridad.RolPermisos | Relacion N:M rol-permiso |
| SesionUsuario | Seguridad.SesionesUsuario | Identificador de sesion y revocacion por cierre |
| Estado | Configuracion.Estados | Catalogo existente consultado, nunca modificado |

Son siete mapeos de tablas YA EXISTENTES, no siete tablas nuevas.
Las cinco tablas solicitadas son el nucleo; SesionesUsuario permite revocar JWT y Estados
permite interpretar correctamente EstadoId. RefreshTokens no se utiliza en esta etapa.

EF usa nombres y esquemas explicitos, PK compuestas en las tablas puente, long para bigint,
nvarchar con limites, datetime2(0), rowversion y relaciones sin borrado en cascada.
Los identificadores bigint se entregan como strings en JSON para no perder precision en JavaScript.
No hay IdentityDbContext, tablas AspNet*, migraciones, EnsureCreated, Migrate ni datos iniciales automaticos.
Configuracion.Estados se protege contra escrituras en este DbContext.

## 4. Registro, sesion y contrasenas

1. POST register valida email, nombre y contrasena; rechaza campos desconocidos como roles.
2. Normaliza email y genera un hash con PasswordHasher<Usuario> (Identity V3, PBKDF2, 210000 iteraciones).
3. En una transaccion crea Usuarios y asigna exclusivamente el rol activo CLIENTE.
4. Resuelve el estado por Grupo=USUARIO, Codigo=ACTIVO; no supone IDs numericos.
5. La cuenta queda EmailConfirmado=false. En esta etapa puede iniciar sesion sin verificar correo.
6. POST login comprueba el hash y el estado ACTIVO, registra SesionesUsuario y ultimo acceso.
7. Emite JWT de 15 minutos en cookie HttpOnly, SameSite=Strict y Secure fuera de Development.
8. Cada peticion autenticada verifica firma, algoritmo, emisor, audiencia, expiracion,
   sesion vigente y estado actual; vuelve a cargar roles activos y permisos desde SQL Server.
9. Logout cierra la sesion persistida. Cambiar contrasena cierra todas las sesiones del usuario.

El JWT contiene sub y jti; no se confia en permisos antiguos dentro de un token.
Ni JWT ni contrasenas se guardan en localStorage. El servidor no devuelve PasswordHash.
El cambio de contrasena requiere conocer la actual. No se cambia email desde el formulario de perfil.
No se migra automaticamente un hash desconocido o una contrasena de la antigua maqueta.
El registro NO crea Cliente.Clientes; ese perfil comercial pertenece a una etapa posterior.

El frontend solicita un token antifalsificacion y lo envia por X-CSRF-TOKEN en todas las escrituras.
La API rechaza escrituras sin un token valido, incluso login y registro. No se habilita CORS abierto.
Hay limite de 10 solicitudes por minuto/IP compartido por registro, login y cambio de contrasena.
El hash se verifica tambien contra una cuenta ficticia cuando no existe el email.

## 5. Roles, permisos y administracion

Los catalogos observados contienen ADMINISTRADOR, CLIENTE y EMPLEADO.
El backend consulta las asignaciones reales; no recrea ni sobrescribe los permisos existentes.

Los endpoints /api/security requieren AMBOS:
- Rol activo ADMINISTRADOR.
- Permiso SEGURIDAD_USUARIOS_GESTIONAR.

Permiten listar usuarios paginados, cambiar su estado, asignar roles, listar/crear roles,
listar/crear permisos y asignar permisos a roles. No hay eliminacion fisica de usuarios
ni un CRUD comercial. El registro publico no acepta un rol elegido por el navegador.

Se impide bloquearse a si mismo, retirarse el propio rol administrador y dejar sin
administrador activo al sistema mediante esas operaciones. Las comprobaciones y cambios
se ejecutan con aislamiento Serializable. El rol ADMINISTRADOR conserva sus permisos
SEGURIDAD_USUARIOS_LEER y SEGURIDAD_USUARIOS_GESTIONAR al editar su asignacion.

Las politicas permission:CODIGO permiten proteger futuros endpoints con permisos de la base,
sin agregar listas de roles a cada controller. Crear un permiso en SQL no crea automaticamente
una funcionalidad: el endpoint futuro debe exigir ese codigo y validar la propiedad del recurso.
Los cambios de rol/permiso se reflejan en la siguiente peticion; no se usa una cache de autorizacion.
No se ha creado una nueva pantalla de administracion: estas operaciones estan disponibles por API.

## 6. Contratos HTTP

Todos los cuerpos son JSON y los nombres usan camelCase. Errores: ProblemDetails, 400/401/403/409/429/503.
Las respuestas de la API llevan Cache-Control: no-store. GET y escrituras no deben cachearse por un proxy.

| Metodo | Ruta | Acceso |
|---|---|---|
| GET | /api/auth/csrf | Anonimo; obtiene token antifalsificacion |
| POST | /api/auth/register | Anonimo + CSRF |
| POST | /api/auth/login | Anonimo + CSRF |
| GET | /api/auth/me | Sesion |
| POST | /api/auth/logout | Sesion + CSRF |
| PUT | /api/auth/profile | Sesion + CSRF |
| POST | /api/auth/change-password | Sesion + CSRF |
| GET | /api/security/users?page=1&pageSize=20 | Administracion |
| PUT | /api/security/users/{id}/state | Administracion + CSRF |
| PUT | /api/security/users/{id}/roles | Administracion + CSRF |
| GET, POST | /api/security/roles | Administracion; POST exige CSRF |
| PUT | /api/security/roles/{id}/permissions | Administracion + CSRF |
| GET, POST | /api/security/permissions | Administracion; POST exige CSRF |
| GET | /health/live | Disponibilidad del proceso |
| GET | /health/ready | Lectura de conexion y catalogos requeridos |
| GET | /openapi/v1.json | Solo Development; documento OpenAPI, no Swagger UI |

Ejemplos de cuerpos: registro { email, password, nombre }; login { email, password };
perfil { nombre }; cambio de clave { passwordActual, passwordNueva };
estado { codigo: "BLOQUEADO" }; roles de usuario { rolIds: [1,2] };
permisos de rol { permisoIds: [1,2] }; rol { codigo, nombre, descripcion };
permiso { codigo, modulo, accion, descripcion }.
Consultar IDs reales antes de asignar: los numeros de esos ejemplos no son valores fijos.

La API del navegador entrega la sesion por cookie, no un access token en el cuerpo.
Aunque JwtBearer valida cabeceras Bearer, esta entrega no incluye un flujo OAuth para terceros.
En un cliente HTTP conservar cookies y obtener CSRF nuevamente despues de login o logout.

## 7. Ejecutar en desarrollo

Requisitos: SDK .NET 10.0.301 compatible, SQL Server/LocalDB con WayPathDB ya existente,
sus catalogos de seguridad y una identidad Windows con permisos de lectura/escritura adecuados.
Ejecutar desde backend/:

```powershell
dotnet restore WayPath.slnx --configfile NuGet.Config
dotnet build WayPath.slnx --no-restore
dotnet test WayPath.slnx --no-restore
```

La clave JWT debe ser aleatoria (al menos 32 bytes) y no debe estar en Git.
Durante esta implementacion se genero una clave local de 64 bytes en User Secrets.
En otra maquina, generar una clave con PowerShell y guardarla sin imprimirla:

```powershell
$bytes = New-Object byte[] 64
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
$key = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:SigningKey" $key --project src/WayPath.Api
Remove-Variable key
dotnet run --project src/WayPath.Api --launch-profile WayPath.Api
```

Abrir http://localhost:5187/login.html o http://localhost:5187/.
Usar ese origen para todas las paginas. Abrir un HTML por file:// o Live Server no conecta esta API.
El puerto puede cambiarse con --urls; no iniciar dos procesos sobre el mismo puerto.
HTTP sin Secure se permite SOLO en Development/localhost. No publicar esa configuracion en Internet.
Fuera de Development, si no se puede redirigir a HTTPS, se rechaza la solicitud HTTP.

appsettings.Development.json usa (localdb)\\MSSQLLocalDB, autenticacion integrada,
Encrypt=True y TrustServerCertificate=True. TrustServerCertificate es solo una comodidad local.
Para otra instancia, sobrescribir ConnectionStrings:WayPathDB mediante User Secrets.
LocalDB pertenece al usuario Windows: ejecutar con otra identidad puede no encontrar la base.
No usar sa ni credenciales predeterminadas para el servicio publicado.

## 8. Primer administrador

No hay usuario administrador o contrasena creados automaticamente por esta implementacion.
Solo si la base aun no tiene ninguna asignacion al rol ADMINISTRADOR, ejecutar en una terminal:

```powershell
dotnet run --project src/WayPath.Api --launch-profile WayPath.Api -- --create-admin
```

Solicita correo, nombre y contrasena oculta con confirmacion. Crea los registros de usuario
y rol dentro de una transaccion, pero NO crea tablas. No admite entrada redirigida.
Si ya existe un administrador, el comando se niega a crear otro: usar la API autenticada.
Una cuenta bloqueada con ese rol requiere un procedimiento controlado de recuperacion por el DBA;
no existe una puerta trasera HTTP para obtener permisos. No ejecutar bootstrap como parte del despliegue.

## 9. Publicacion y seguridad pendiente

```powershell
dotnet publish src/WayPath.Api -c Release
```

El frontend se copia a wwwroot dentro del resultado publicado. En produccion configurar
ASPNETCORE_ENVIRONMENT=Production, ConnectionStrings__WayPathDB, Jwt__SigningKey,
Jwt__Issuer, Jwt__Audience y AllowedHosts mediante la plataforma/gestor de secretos.
User Secrets es solo desarrollo. appsettings.Local.json, si se usa, se excluye de publicacion.

Antes de uso productivo son necesarios:
- HTTPS con certificado valido y SQL Server con certificado confiable.
- Cuenta SQL/Windows de minimo privilegio: DML solo en las tablas del modulo y SELECT en Estados.
- Configurar proxy confiable y forwarded headers si TLS termina en un proxy. No confiar en IP enviadas por cualquiera.
- Persistir/proteger y compartir claves de Data Protection para CSRF al escalar a varias instancias.
- Compartir limitacion de intentos entre instancias, vigilancia de ataques distribuidos y proteccion por cuenta.
- Verificacion de correo, recuperacion de contrasena, MFA, politica de sesiones/renovacion y depuracion de sesiones antiguas.
- Auditoria durable de cambios administrativos, alertas y pruebas de concurrencia contra una base aislada.
- Rotacion de claves JWT, copias de seguridad, restauracion verificada, observabilidad y configuracion de retencion.
- Una CSP mas estricta: el frontend existente conserva scripts inline y dependencias externas.
- Autorizacion por recurso cuando existan reservas/clientes reales; un permiso general no concede acceso a todos los registros.

No se presenta esta primera etapa como un sistema empresarial terminado. No hay envio de correos,
refresh token, recuperacion automatica ni implementacion de modulos de negocio aun.

## 10. Pruebas y limites de verificacion

- Compilacion: correcta, sin advertencias.
- Publicacion Release: completada; incluye frontend y API.
- 27 pruebas xUnit: autenticacion, validacion, CSRF entre navegadores, HTTPS obligatorio,
  cookies seguras en produccion, intentos, tokens falsos/vencidos,
  revocacion, cambios de contrasena, permisos actuales, DTOs y metadatos EF.
- La suite HTTP usa el pipeline real de ASP.NET Core y un ISecurityStore aislado en memoria.
  Nunca crea tablas ni usa usuarios de WayPathDB.
- SQL Server real: comprobacion de conexion/catalogos de solo lectura y login con correo
  de prueba inexistente. No se crearon cuentas de prueba ni se modificaron registros reales.
- Navegador Edge/Playwright: index/login/registro en 1366x900 y 390x844,
  sin errores JavaScript ni desbordamiento horizontal. Las preferencias de maqueta se conservan.
- cuenta.html se reviso con una respuesta de API simulada exclusivamente en el navegador de prueba.
  Esa comprobacion visual no equivale a una escritura real en SQL Server.
- Falta ejecutar casos positivos de persistencia, competencia entre transacciones y administracion
  sobre una copia de pruebas de WayPathDB con datos de prueba expresamente autorizados.

## 11. Integracion progresiva posterior

1. Consolidar Seguridad: administrador inicial, pruebas de aceptacion y endurecimiento indicado.
2. Catalogo de solo lectura: reemplazar arreglos simulados por DTOs/API manteniendo HTML.
3. Perfil comercial de cliente y pasajeros sobre tablas existentes, separado de Usuario.
4. Operacion y disponibilidad: salidas/cupos con transacciones y control de concurrencia.
5. Reservas y ventas: reglas, propiedad de datos, cancelacion y estados reales.
6. Pagos: pasarela, idempotencia, firma de webhooks, conciliacion y reembolsos.
7. Marketing, soporte, reportes y auditoria con autorizacion especifica.

Estas etapas son propuestas, no codigo ya implementado. Antes de mapear cada modulo
debe verificarse otra vez el esquema real y sus restricciones; no se agregaran tablas por suposicion.

## Referencias tecnicas

- [Proveedor SQL Server de EF Core](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)
- [Concurrencia en EF Core](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [Proteccion antifalsificacion en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)
- [Aplicacion controlada de migraciones](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
