USE master;
GO

IF DB_ID(N'WayPathDB') IS NOT NULL
BEGIN
    ALTER DATABASE WayPathDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE WayPathDB;
END
GO

EXEC(N'CREATE DATABASE WayPathDB;');
GO

USE WayPathDB;
GO

SET NOCOUNT ON;
GO

CREATE SCHEMA Seguridad AUTHORIZATION dbo;
GO
CREATE SCHEMA Cliente AUTHORIZATION dbo;
GO
CREATE SCHEMA Catalogo AUTHORIZATION dbo;
GO
CREATE SCHEMA Operacion AUTHORIZATION dbo;
GO
CREATE SCHEMA Ventas AUTHORIZATION dbo;
GO
CREATE SCHEMA Pagos AUTHORIZATION dbo;
GO
CREATE SCHEMA Marketing AUTHORIZATION dbo;
GO
CREATE SCHEMA Soporte AUTHORIZATION dbo;
GO
CREATE SCHEMA Reportes AUTHORIZATION dbo;
GO
CREATE SCHEMA Auditoria AUTHORIZATION dbo;
GO
CREATE SCHEMA Configuracion AUTHORIZATION dbo;
GO

CREATE TABLE Configuracion.Estados (
    EstadoId INT IDENTITY(1,1) NOT NULL,
    Grupo NVARCHAR(50) NOT NULL,
    Codigo NVARCHAR(50) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_Estados_EsActivo DEFAULT (1),
    Orden INT NOT NULL CONSTRAINT DF_Estados_Orden DEFAULT (0),
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Estados_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Estados PRIMARY KEY (EstadoId),
    CONSTRAINT UQ_Estados_Grupo_Codigo UNIQUE (Grupo, Codigo),
    CONSTRAINT CK_Estados_Grupo CHECK (LEN(LTRIM(RTRIM(Grupo))) > 0),
    CONSTRAINT CK_Estados_Codigo CHECK (LEN(LTRIM(RTRIM(Codigo))) > 0)
);
GO

CREATE TABLE Configuracion.TiposDocumento (
    TipoDocumentoId INT IDENTITY(1,1) NOT NULL,
    Codigo NVARCHAR(30) NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    LongitudMinima INT NULL,
    LongitudMaxima INT NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_TiposDocumento_EsActivo DEFAULT (1),
    CONSTRAINT PK_TiposDocumento PRIMARY KEY (TipoDocumentoId),
    CONSTRAINT UQ_TiposDocumento_Codigo UNIQUE (Codigo),
    CONSTRAINT CK_TiposDocumento_Longitud CHECK (
        LongitudMinima IS NULL OR LongitudMaxima IS NULL OR LongitudMaxima >= LongitudMinima
    )
);
GO

CREATE TABLE Configuracion.Monedas (
    MonedaId INT IDENTITY(1,1) NOT NULL,
    CodigoIso NVARCHAR(3) NOT NULL,
    Nombre NVARCHAR(80) NOT NULL,
    Simbolo NVARCHAR(10) NOT NULL,
    EsMonedaBase BIT NOT NULL CONSTRAINT DF_Monedas_EsMonedaBase DEFAULT (0),
    EsActivo BIT NOT NULL CONSTRAINT DF_Monedas_EsActivo DEFAULT (1),
    CONSTRAINT PK_Monedas PRIMARY KEY (MonedaId),
    CONSTRAINT UQ_Monedas_CodigoIso UNIQUE (CodigoIso),
    CONSTRAINT CK_Monedas_CodigoIso CHECK (LEN(CodigoIso) = 3)
);
GO

CREATE TABLE Configuracion.ParametrosSistema (
    ParametroId INT IDENTITY(1,1) NOT NULL,
    Clave NVARCHAR(100) NOT NULL,
    Valor NVARCHAR(500) NOT NULL,
    TipoDato NVARCHAR(30) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    EsEditable BIT NOT NULL CONSTRAINT DF_ParametrosSistema_EsEditable DEFAULT (1),
    ActualizadoEn DATETIME2(0) NULL,
    CONSTRAINT PK_ParametrosSistema PRIMARY KEY (ParametroId),
    CONSTRAINT UQ_ParametrosSistema_Clave UNIQUE (Clave),
    CONSTRAINT CK_ParametrosSistema_TipoDato CHECK (TipoDato IN ('STRING','INT','DECIMAL','BOOL','DATE','JSON'))
);
GO

CREATE TABLE Configuracion.TerminosLegales (
    TerminoLegalId INT IDENTITY(1,1) NOT NULL,
    Tipo NVARCHAR(40) NOT NULL,
    Version NVARCHAR(30) NOT NULL,
    Titulo NVARCHAR(150) NOT NULL,
    Contenido NVARCHAR(MAX) NOT NULL,
    EsVigente BIT NOT NULL CONSTRAINT DF_TerminosLegales_EsVigente DEFAULT (0),
    PublicadoEn DATETIME2(0) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_TerminosLegales_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TerminosLegales PRIMARY KEY (TerminoLegalId),
    CONSTRAINT UQ_TerminosLegales_Tipo_Version UNIQUE (Tipo, Version),
    CONSTRAINT CK_TerminosLegales_Tipo CHECK (Tipo IN ('PRIVACIDAD','TERMINOS','COOKIES','CONTRATO_VIAJE'))
);
GO

CREATE TABLE Configuracion.PoliticasCancelacion (
    PoliticaCancelacionId INT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    HorasAntesInicio INT NOT NULL,
    PorcentajePenalidad DECIMAL(5,2) NOT NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_PoliticasCancelacion_EsActivo DEFAULT (1),
    CONSTRAINT PK_PoliticasCancelacion PRIMARY KEY (PoliticaCancelacionId),
    CONSTRAINT CK_PoliticasCancelacion_Horas CHECK (HorasAntesInicio >= 0),
    CONSTRAINT CK_PoliticasCancelacion_Penalidad CHECK (PorcentajePenalidad BETWEEN 0 AND 100)
);
GO

CREATE TABLE Seguridad.Usuarios (
    UsuarioId BIGINT IDENTITY(1,1) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    NombreUsuario NVARCHAR(100) NULL,
    EstadoId INT NOT NULL,
    EmailConfirmado BIT NOT NULL CONSTRAINT DF_Usuarios_EmailConfirmado DEFAULT (0),
    TelefonoConfirmado BIT NOT NULL CONSTRAINT DF_Usuarios_TelefonoConfirmado DEFAULT (0),
    UltimoAccesoEn DATETIME2(0) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Usuarios_CreadoEn DEFAULT SYSUTCDATETIME(),
    ActualizadoEn DATETIME2(0) NULL,
    FilaVersion ROWVERSION,
    CONSTRAINT PK_Usuarios PRIMARY KEY (UsuarioId),
    CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),
    CONSTRAINT FK_Usuarios_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Usuarios_Email CHECK (Email LIKE '%_@_%._%')
);
GO

CREATE TABLE Seguridad.Roles (
    RolId INT IDENTITY(1,1) NOT NULL,
    Codigo NVARCHAR(50) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    EsSistema BIT NOT NULL CONSTRAINT DF_Roles_EsSistema DEFAULT (0),
    EsActivo BIT NOT NULL CONSTRAINT DF_Roles_EsActivo DEFAULT (1),
    CONSTRAINT PK_Roles PRIMARY KEY (RolId),
    CONSTRAINT UQ_Roles_Codigo UNIQUE (Codigo)
);
GO

CREATE TABLE Seguridad.UsuarioRoles (
    UsuarioId BIGINT NOT NULL,
    RolId INT NOT NULL,
    AsignadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_UsuarioRoles_AsignadoEn DEFAULT SYSUTCDATETIME(),
    AsignadoPorUsuarioId BIGINT NULL,
    CONSTRAINT PK_UsuarioRoles PRIMARY KEY (UsuarioId, RolId),
    CONSTRAINT FK_UsuarioRoles_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT FK_UsuarioRoles_Roles FOREIGN KEY (RolId) REFERENCES Seguridad.Roles(RolId),
    CONSTRAINT FK_UsuarioRoles_AsignadoPor FOREIGN KEY (AsignadoPorUsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId)
);
GO

CREATE TABLE Seguridad.Permisos (
    PermisoId INT IDENTITY(1,1) NOT NULL,
    Codigo NVARCHAR(100) NOT NULL,
    Modulo NVARCHAR(80) NOT NULL,
    Accion NVARCHAR(80) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    CONSTRAINT PK_Permisos PRIMARY KEY (PermisoId),
    CONSTRAINT UQ_Permisos_Codigo UNIQUE (Codigo)
);
GO

CREATE TABLE Seguridad.RolPermisos (
    RolId INT NOT NULL,
    PermisoId INT NOT NULL,
    CONSTRAINT PK_RolPermisos PRIMARY KEY (RolId, PermisoId),
    CONSTRAINT FK_RolPermisos_Roles FOREIGN KEY (RolId) REFERENCES Seguridad.Roles(RolId),
    CONSTRAINT FK_RolPermisos_Permisos FOREIGN KEY (PermisoId) REFERENCES Seguridad.Permisos(PermisoId)
);
GO

CREATE TABLE Seguridad.RefreshTokens (
    RefreshTokenId BIGINT IDENTITY(1,1) NOT NULL,
    UsuarioId BIGINT NOT NULL,
    TokenHash VARBINARY(64) NOT NULL,
    ExpiraEn DATETIME2(0) NOT NULL,
    RevocadoEn DATETIME2(0) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_RefreshTokens_CreadoEn DEFAULT SYSUTCDATETIME(),
    CreadoPorIp NVARCHAR(45) NULL,
    CONSTRAINT PK_RefreshTokens PRIMARY KEY (RefreshTokenId),
    CONSTRAINT FK_RefreshTokens_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT UQ_RefreshTokens_TokenHash UNIQUE (TokenHash),
    CONSTRAINT CK_RefreshTokens_Expira CHECK (ExpiraEn > CreadoEn)
);
GO

CREATE TABLE Seguridad.SesionesUsuario (
    SesionUsuarioId BIGINT IDENTITY(1,1) NOT NULL,
    UsuarioId BIGINT NOT NULL,
    IdentificadorSesion NVARCHAR(120) NOT NULL,
    Ip NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    IniciadaEn DATETIME2(0) NOT NULL CONSTRAINT DF_SesionesUsuario_IniciadaEn DEFAULT SYSUTCDATETIME(),
    FinalizadaEn DATETIME2(0) NULL,
    CONSTRAINT PK_SesionesUsuario PRIMARY KEY (SesionUsuarioId),
    CONSTRAINT FK_SesionesUsuario_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT UQ_SesionesUsuario_Identificador UNIQUE (IdentificadorSesion),
    CONSTRAINT CK_SesionesUsuario_Fechas CHECK (FinalizadaEn IS NULL OR FinalizadaEn >= IniciadaEn)
);
GO

CREATE TABLE Operacion.Areas (
    AreaId INT IDENTITY(1,1) NOT NULL,
    Codigo NVARCHAR(50) NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_Areas_EsActivo DEFAULT (1),
    CONSTRAINT PK_Areas PRIMARY KEY (AreaId),
    CONSTRAINT UQ_Areas_Codigo UNIQUE (Codigo)
);
GO

CREATE TABLE Operacion.Empleados (
    EmpleadoId BIGINT IDENTITY(1,1) NOT NULL,
    UsuarioId BIGINT NOT NULL,
    AreaId INT NOT NULL,
    CodigoEmpleado NVARCHAR(50) NOT NULL,
    Cargo NVARCHAR(120) NOT NULL,
    EstadoId INT NOT NULL,
    FechaIngreso DATE NOT NULL,
    FechaSalida DATE NULL,
    CONSTRAINT PK_Empleados PRIMARY KEY (EmpleadoId),
    CONSTRAINT UQ_Empleados_Usuario UNIQUE (UsuarioId),
    CONSTRAINT UQ_Empleados_Codigo UNIQUE (CodigoEmpleado),
    CONSTRAINT FK_Empleados_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT FK_Empleados_Areas FOREIGN KEY (AreaId) REFERENCES Operacion.Areas(AreaId),
    CONSTRAINT FK_Empleados_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Empleados_Fechas CHECK (FechaSalida IS NULL OR FechaSalida >= FechaIngreso)
);
GO

CREATE TABLE Catalogo.Paises (
    PaisId INT IDENTITY(1,1) NOT NULL,
    CodigoIso2 NVARCHAR(2) NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_Paises_EsActivo DEFAULT (1),
    CONSTRAINT PK_Paises PRIMARY KEY (PaisId),
    CONSTRAINT UQ_Paises_CodigoIso2 UNIQUE (CodigoIso2),
    CONSTRAINT UQ_Paises_Nombre UNIQUE (Nombre),
    CONSTRAINT CK_Paises_CodigoIso2 CHECK (LEN(CodigoIso2) = 2)
);
GO

CREATE TABLE Catalogo.Regiones (
    RegionId INT IDENTITY(1,1) NOT NULL,
    PaisId INT NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_Regiones_EsActivo DEFAULT (1),
    CONSTRAINT PK_Regiones PRIMARY KEY (RegionId),
    CONSTRAINT FK_Regiones_Paises FOREIGN KEY (PaisId) REFERENCES Catalogo.Paises(PaisId),
    CONSTRAINT UQ_Regiones_Pais_Nombre UNIQUE (PaisId, Nombre)
);
GO

CREATE TABLE Catalogo.Ciudades (
    CiudadId INT IDENTITY(1,1) NOT NULL,
    RegionId INT NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_Ciudades_EsActivo DEFAULT (1),
    CONSTRAINT PK_Ciudades PRIMARY KEY (CiudadId),
    CONSTRAINT FK_Ciudades_Regiones FOREIGN KEY (RegionId) REFERENCES Catalogo.Regiones(RegionId),
    CONSTRAINT UQ_Ciudades_Region_Nombre UNIQUE (RegionId, Nombre)
);
GO

CREATE TABLE Catalogo.MediaAssets (
    MediaAssetId BIGINT IDENTITY(1,1) NOT NULL,
    Tipo NVARCHAR(30) NOT NULL,
    Url NVARCHAR(1000) NOT NULL,
    NombreArchivo NVARCHAR(255) NULL,
    MimeType NVARCHAR(100) NULL,
    TextoAlternativo NVARCHAR(250) NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_MediaAssets_EsActivo DEFAULT (1),
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_MediaAssets_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_MediaAssets PRIMARY KEY (MediaAssetId),
    CONSTRAINT CK_MediaAssets_Tipo CHECK (Tipo IN ('IMAGEN','VIDEO','DOCUMENTO','ICONO'))
);
GO

CREATE TABLE Catalogo.UbicacionesTuristicas (
    UbicacionTuristicaId BIGINT IDENTITY(1,1) NOT NULL,
    CiudadId INT NOT NULL,
    Nombre NVARCHAR(150) NOT NULL,
    Direccion NVARCHAR(250) NULL,
    Latitud DECIMAL(9,6) NULL,
    Longitud DECIMAL(9,6) NULL,
    CONSTRAINT PK_UbicacionesTuristicas PRIMARY KEY (UbicacionTuristicaId),
    CONSTRAINT FK_UbicacionesTuristicas_Ciudades FOREIGN KEY (CiudadId) REFERENCES Catalogo.Ciudades(CiudadId),
    CONSTRAINT CK_UbicacionesTuristicas_Latitud CHECK (Latitud IS NULL OR Latitud BETWEEN -90 AND 90),
    CONSTRAINT CK_UbicacionesTuristicas_Longitud CHECK (Longitud IS NULL OR Longitud BETWEEN -180 AND 180)
);
GO

CREATE TABLE Catalogo.Destinos (
    DestinoId BIGINT IDENTITY(1,1) NOT NULL,
    CiudadId INT NOT NULL,
    UbicacionTuristicaId BIGINT NULL,
    EstadoId INT NOT NULL,
    Nombre NVARCHAR(160) NOT NULL,
    Slug NVARCHAR(180) NOT NULL,
    DescripcionCorta NVARCHAR(300) NOT NULL,
    DescripcionLarga NVARCHAR(MAX) NULL,
    AltitudMetros INT NULL,
    EsDestacado BIT NOT NULL CONSTRAINT DF_Destinos_EsDestacado DEFAULT (0),
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Destinos_CreadoEn DEFAULT SYSUTCDATETIME(),
    ActualizadoEn DATETIME2(0) NULL,
    FilaVersion ROWVERSION,
    CONSTRAINT PK_Destinos PRIMARY KEY (DestinoId),
    CONSTRAINT UQ_Destinos_Slug UNIQUE (Slug),
    CONSTRAINT FK_Destinos_Ciudades FOREIGN KEY (CiudadId) REFERENCES Catalogo.Ciudades(CiudadId),
    CONSTRAINT FK_Destinos_Ubicaciones FOREIGN KEY (UbicacionTuristicaId) REFERENCES Catalogo.UbicacionesTuristicas(UbicacionTuristicaId),
    CONSTRAINT FK_Destinos_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Destinos_Altitud CHECK (AltitudMetros IS NULL OR AltitudMetros >= 0)
);
GO

CREATE TABLE Catalogo.CategoriasDestino (
    CategoriaDestinoId INT IDENTITY(1,1) NOT NULL,
    Codigo NVARCHAR(50) NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    Descripcion NVARCHAR(250) NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_CategoriasDestino_EsActivo DEFAULT (1),
    CONSTRAINT PK_CategoriasDestino PRIMARY KEY (CategoriaDestinoId),
    CONSTRAINT UQ_CategoriasDestino_Codigo UNIQUE (Codigo)
);
GO

CREATE TABLE Catalogo.DestinoCategorias (
    DestinoId BIGINT NOT NULL,
    CategoriaDestinoId INT NOT NULL,
    CONSTRAINT PK_DestinoCategorias PRIMARY KEY (DestinoId, CategoriaDestinoId),
    CONSTRAINT FK_DestinoCategorias_Destinos FOREIGN KEY (DestinoId) REFERENCES Catalogo.Destinos(DestinoId),
    CONSTRAINT FK_DestinoCategorias_Categorias FOREIGN KEY (CategoriaDestinoId) REFERENCES Catalogo.CategoriasDestino(CategoriaDestinoId)
);
GO

CREATE TABLE Catalogo.DestinoImagenes (
    DestinoImagenId BIGINT IDENTITY(1,1) NOT NULL,
    DestinoId BIGINT NOT NULL,
    MediaAssetId BIGINT NOT NULL,
    EsPrincipal BIT NOT NULL CONSTRAINT DF_DestinoImagenes_EsPrincipal DEFAULT (0),
    Orden INT NOT NULL CONSTRAINT DF_DestinoImagenes_Orden DEFAULT (0),
    CONSTRAINT PK_DestinoImagenes PRIMARY KEY (DestinoImagenId),
    CONSTRAINT FK_DestinoImagenes_Destinos FOREIGN KEY (DestinoId) REFERENCES Catalogo.Destinos(DestinoId),
    CONSTRAINT FK_DestinoImagenes_MediaAssets FOREIGN KEY (MediaAssetId) REFERENCES Catalogo.MediaAssets(MediaAssetId)
);
GO

CREATE TABLE Catalogo.AtraccionesDestino (
    AtraccionDestinoId BIGINT IDENTITY(1,1) NOT NULL,
    DestinoId BIGINT NOT NULL,
    Nombre NVARCHAR(160) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    DuracionEstimadaMinutos INT NULL,
    Orden INT NOT NULL CONSTRAINT DF_AtraccionesDestino_Orden DEFAULT (0),
    CONSTRAINT PK_AtraccionesDestino PRIMARY KEY (AtraccionDestinoId),
    CONSTRAINT FK_AtraccionesDestino_Destinos FOREIGN KEY (DestinoId) REFERENCES Catalogo.Destinos(DestinoId),
    CONSTRAINT CK_AtraccionesDestino_Duracion CHECK (DuracionEstimadaMinutos IS NULL OR DuracionEstimadaMinutos > 0)
);
GO

CREATE TABLE Catalogo.Paquetes (
    PaqueteId BIGINT IDENTITY(1,1) NOT NULL,
    DestinoId BIGINT NOT NULL,
    EstadoId INT NOT NULL,
    Nombre NVARCHAR(180) NOT NULL,
    Slug NVARCHAR(200) NOT NULL,
    Descripcion NVARCHAR(MAX) NULL,
    DuracionDias INT NOT NULL,
    DuracionNoches INT NOT NULL,
    NivelDificultad NVARCHAR(30) NULL,
    EsDestacado BIT NOT NULL CONSTRAINT DF_Paquetes_EsDestacado DEFAULT (0),
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Paquetes_CreadoEn DEFAULT SYSUTCDATETIME(),
    ActualizadoEn DATETIME2(0) NULL,
    FilaVersion ROWVERSION,
    CONSTRAINT PK_Paquetes PRIMARY KEY (PaqueteId),
    CONSTRAINT UQ_Paquetes_Slug UNIQUE (Slug),
    CONSTRAINT FK_Paquetes_Destinos FOREIGN KEY (DestinoId) REFERENCES Catalogo.Destinos(DestinoId),
    CONSTRAINT FK_Paquetes_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Paquetes_DuracionDias CHECK (DuracionDias > 0),
    CONSTRAINT CK_Paquetes_DuracionNoches CHECK (DuracionNoches >= 0),
    CONSTRAINT CK_Paquetes_Nivel CHECK (NivelDificultad IS NULL OR NivelDificultad IN ('BAJO','MEDIO','ALTO','EXTREMO'))
);
GO

CREATE TABLE Catalogo.PaqueteItinerario (
    PaqueteItinerarioId BIGINT IDENTITY(1,1) NOT NULL,
    PaqueteId BIGINT NOT NULL,
    DiaNumero INT NOT NULL,
    Titulo NVARCHAR(160) NOT NULL,
    Descripcion NVARCHAR(MAX) NOT NULL,
    Orden INT NOT NULL CONSTRAINT DF_PaqueteItinerario_Orden DEFAULT (0),
    CONSTRAINT PK_PaqueteItinerario PRIMARY KEY (PaqueteItinerarioId),
    CONSTRAINT FK_PaqueteItinerario_Paquetes FOREIGN KEY (PaqueteId) REFERENCES Catalogo.Paquetes(PaqueteId),
    CONSTRAINT UQ_PaqueteItinerario_Paquete_Dia_Orden UNIQUE (PaqueteId, DiaNumero, Orden),
    CONSTRAINT CK_PaqueteItinerario_Dia CHECK (DiaNumero > 0)
);
GO

CREATE TABLE Catalogo.PaqueteIncluye (
    PaqueteIncluyeId BIGINT IDENTITY(1,1) NOT NULL,
    PaqueteId BIGINT NOT NULL,
    Descripcion NVARCHAR(250) NOT NULL,
    Orden INT NOT NULL CONSTRAINT DF_PaqueteIncluye_Orden DEFAULT (0),
    CONSTRAINT PK_PaqueteIncluye PRIMARY KEY (PaqueteIncluyeId),
    CONSTRAINT FK_PaqueteIncluye_Paquetes FOREIGN KEY (PaqueteId) REFERENCES Catalogo.Paquetes(PaqueteId)
);
GO

CREATE TABLE Catalogo.PaqueteNoIncluye (
    PaqueteNoIncluyeId BIGINT IDENTITY(1,1) NOT NULL,
    PaqueteId BIGINT NOT NULL,
    Descripcion NVARCHAR(250) NOT NULL,
    Orden INT NOT NULL CONSTRAINT DF_PaqueteNoIncluye_Orden DEFAULT (0),
    CONSTRAINT PK_PaqueteNoIncluye PRIMARY KEY (PaqueteNoIncluyeId),
    CONSTRAINT FK_PaqueteNoIncluye_Paquetes FOREIGN KEY (PaqueteId) REFERENCES Catalogo.Paquetes(PaqueteId)
);
GO

CREATE TABLE Catalogo.PaquetePoliticas (
    PaquetePoliticaId BIGINT IDENTITY(1,1) NOT NULL,
    PaqueteId BIGINT NOT NULL,
    PoliticaCancelacionId INT NOT NULL,
    EsPrincipal BIT NOT NULL CONSTRAINT DF_PaquetePoliticas_EsPrincipal DEFAULT (0),
    CONSTRAINT PK_PaquetePoliticas PRIMARY KEY (PaquetePoliticaId),
    CONSTRAINT FK_PaquetePoliticas_Paquetes FOREIGN KEY (PaqueteId) REFERENCES Catalogo.Paquetes(PaqueteId),
    CONSTRAINT FK_PaquetePoliticas_Politicas FOREIGN KEY (PoliticaCancelacionId) REFERENCES Configuracion.PoliticasCancelacion(PoliticaCancelacionId)
);
GO

CREATE TABLE Catalogo.Tarifas (
    TarifaId BIGINT IDENTITY(1,1) NOT NULL,
    PaqueteId BIGINT NOT NULL,
    MonedaId INT NOT NULL,
    EstadoId INT NOT NULL,
    TipoTarifa NVARCHAR(40) NOT NULL,
    PrecioBase DECIMAL(18,2) NOT NULL,
    PrecioNino DECIMAL(18,2) NULL,
    VigenciaInicio DATE NOT NULL,
    VigenciaFin DATE NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Tarifas_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Tarifas PRIMARY KEY (TarifaId),
    CONSTRAINT FK_Tarifas_Paquetes FOREIGN KEY (PaqueteId) REFERENCES Catalogo.Paquetes(PaqueteId),
    CONSTRAINT FK_Tarifas_Monedas FOREIGN KEY (MonedaId) REFERENCES Configuracion.Monedas(MonedaId),
    CONSTRAINT FK_Tarifas_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT UQ_Tarifas_Paquete_Tipo_Inicio UNIQUE (PaqueteId, TipoTarifa, VigenciaInicio),
    CONSTRAINT CK_Tarifas_PrecioBase CHECK (PrecioBase >= 0),
    CONSTRAINT CK_Tarifas_PrecioNino CHECK (PrecioNino IS NULL OR PrecioNino >= 0),
    CONSTRAINT CK_Tarifas_Fechas CHECK (VigenciaFin IS NULL OR VigenciaFin >= VigenciaInicio)
);
GO

CREATE TABLE Catalogo.Impuestos (
    ImpuestoId INT IDENTITY(1,1) NOT NULL,
    Codigo NVARCHAR(50) NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    Porcentaje DECIMAL(7,4) NOT NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_Impuestos_EsActivo DEFAULT (1),
    CONSTRAINT PK_Impuestos PRIMARY KEY (ImpuestoId),
    CONSTRAINT UQ_Impuestos_Codigo UNIQUE (Codigo),
    CONSTRAINT CK_Impuestos_Porcentaje CHECK (Porcentaje >= 0 AND Porcentaje <= 100)
);
GO

CREATE TABLE Catalogo.TarifaImpuestos (
    TarifaId BIGINT NOT NULL,
    ImpuestoId INT NOT NULL,
    CONSTRAINT PK_TarifaImpuestos PRIMARY KEY (TarifaId, ImpuestoId),
    CONSTRAINT FK_TarifaImpuestos_Tarifas FOREIGN KEY (TarifaId) REFERENCES Catalogo.Tarifas(TarifaId),
    CONSTRAINT FK_TarifaImpuestos_Impuestos FOREIGN KEY (ImpuestoId) REFERENCES Catalogo.Impuestos(ImpuestoId)
);
GO

CREATE TABLE Catalogo.PaginasContenido (
    PaginaContenidoId BIGINT IDENTITY(1,1) NOT NULL,
    Slug NVARCHAR(160) NOT NULL,
    Titulo NVARCHAR(180) NOT NULL,
    ContenidoHtml NVARCHAR(MAX) NOT NULL,
    EstadoId INT NOT NULL,
    PublicadoEn DATETIME2(0) NULL,
    CONSTRAINT PK_PaginasContenido PRIMARY KEY (PaginaContenidoId),
    CONSTRAINT UQ_PaginasContenido_Slug UNIQUE (Slug),
    CONSTRAINT FK_PaginasContenido_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId)
);
GO

CREATE TABLE Catalogo.Banners (
    BannerId BIGINT IDENTITY(1,1) NOT NULL,
    PaginaContenidoId BIGINT NULL,
    MediaAssetId BIGINT NULL,
    Titulo NVARCHAR(180) NOT NULL,
    Subtitulo NVARCHAR(300) NULL,
    UrlDestino NVARCHAR(1000) NULL,
    FechaInicio DATETIME2(0) NULL,
    FechaFin DATETIME2(0) NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_Banners_EsActivo DEFAULT (1),
    CONSTRAINT PK_Banners PRIMARY KEY (BannerId),
    CONSTRAINT FK_Banners_PaginasContenido FOREIGN KEY (PaginaContenidoId) REFERENCES Catalogo.PaginasContenido(PaginaContenidoId),
    CONSTRAINT FK_Banners_MediaAssets FOREIGN KEY (MediaAssetId) REFERENCES Catalogo.MediaAssets(MediaAssetId),
    CONSTRAINT CK_Banners_Fechas CHECK (FechaFin IS NULL OR FechaInicio IS NULL OR FechaFin >= FechaInicio)
);
GO

CREATE TABLE Catalogo.FAQs (
    FAQId BIGINT IDENTITY(1,1) NOT NULL,
    PaginaContenidoId BIGINT NULL,
    Pregunta NVARCHAR(300) NOT NULL,
    Respuesta NVARCHAR(MAX) NOT NULL,
    Orden INT NOT NULL CONSTRAINT DF_FAQs_Orden DEFAULT (0),
    EsActivo BIT NOT NULL CONSTRAINT DF_FAQs_EsActivo DEFAULT (1),
    CONSTRAINT PK_FAQs PRIMARY KEY (FAQId),
    CONSTRAINT FK_FAQs_PaginasContenido FOREIGN KEY (PaginaContenidoId) REFERENCES Catalogo.PaginasContenido(PaginaContenidoId)
);
GO

CREATE TABLE Catalogo.MenuNavegacion (
    MenuNavegacionId INT IDENTITY(1,1) NOT NULL,
    MenuPadreId INT NULL,
    Texto NVARCHAR(120) NOT NULL,
    Url NVARCHAR(500) NOT NULL,
    Icono NVARCHAR(80) NULL,
    Orden INT NOT NULL CONSTRAINT DF_MenuNavegacion_Orden DEFAULT (0),
    EsActivo BIT NOT NULL CONSTRAINT DF_MenuNavegacion_EsActivo DEFAULT (1),
    CONSTRAINT PK_MenuNavegacion PRIMARY KEY (MenuNavegacionId),
    CONSTRAINT FK_MenuNavegacion_Padre FOREIGN KEY (MenuPadreId) REFERENCES Catalogo.MenuNavegacion(MenuNavegacionId)
);
GO

CREATE TABLE Cliente.Clientes (
    ClienteId BIGINT IDENTITY(1,1) NOT NULL,
    UsuarioId BIGINT NOT NULL,
    EstadoId INT NOT NULL,
    Nombres NVARCHAR(120) NOT NULL,
    Apellidos NVARCHAR(120) NOT NULL,
    Telefono NVARCHAR(30) NULL,
    FechaNacimiento DATE NULL,
    Genero NVARCHAR(20) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Clientes_CreadoEn DEFAULT SYSUTCDATETIME(),
    ActualizadoEn DATETIME2(0) NULL,
    FilaVersion ROWVERSION,
    CONSTRAINT PK_Clientes PRIMARY KEY (ClienteId),
    CONSTRAINT UQ_Clientes_Usuario UNIQUE (UsuarioId),
    CONSTRAINT FK_Clientes_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT FK_Clientes_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Clientes_Genero CHECK (Genero IS NULL OR Genero IN ('MASCULINO','FEMENINO','OTRO','NO_DECLARA'))
);
GO

CREATE TABLE Cliente.DocumentosCliente (
    DocumentoClienteId BIGINT IDENTITY(1,1) NOT NULL,
    ClienteId BIGINT NOT NULL,
    TipoDocumentoId INT NOT NULL,
    NumeroDocumento NVARCHAR(40) NOT NULL,
    PaisEmisionId INT NULL,
    FechaVencimiento DATE NULL,
    EsPrincipal BIT NOT NULL CONSTRAINT DF_DocumentosCliente_EsPrincipal DEFAULT (0),
    CONSTRAINT PK_DocumentosCliente PRIMARY KEY (DocumentoClienteId),
    CONSTRAINT FK_DocumentosCliente_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_DocumentosCliente_TiposDocumento FOREIGN KEY (TipoDocumentoId) REFERENCES Configuracion.TiposDocumento(TipoDocumentoId),
    CONSTRAINT FK_DocumentosCliente_Paises FOREIGN KEY (PaisEmisionId) REFERENCES Catalogo.Paises(PaisId),
    CONSTRAINT UQ_DocumentosCliente_Tipo_Numero UNIQUE (TipoDocumentoId, NumeroDocumento)
);
GO

CREATE TABLE Cliente.DireccionesCliente (
    DireccionClienteId BIGINT IDENTITY(1,1) NOT NULL,
    ClienteId BIGINT NOT NULL,
    CiudadId INT NOT NULL,
    LineaDireccion NVARCHAR(250) NOT NULL,
    CodigoPostal NVARCHAR(20) NULL,
    EsPrincipal BIT NOT NULL CONSTRAINT DF_DireccionesCliente_EsPrincipal DEFAULT (0),
    CONSTRAINT PK_DireccionesCliente PRIMARY KEY (DireccionClienteId),
    CONSTRAINT FK_DireccionesCliente_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_DireccionesCliente_Ciudades FOREIGN KEY (CiudadId) REFERENCES Catalogo.Ciudades(CiudadId)
);
GO

CREATE TABLE Cliente.PreferenciasCliente (
    PreferenciaClienteId BIGINT IDENTITY(1,1) NOT NULL,
    ClienteId BIGINT NOT NULL,
    IdiomaPreferido NVARCHAR(10) NOT NULL CONSTRAINT DF_PreferenciasCliente_Idioma DEFAULT ('es'),
    MonedaPreferidaId INT NULL,
    RecibePromociones BIT NOT NULL CONSTRAINT DF_PreferenciasCliente_RecibePromociones DEFAULT (1),
    RequiereAccesibilidad BIT NOT NULL CONSTRAINT DF_PreferenciasCliente_Accesibilidad DEFAULT (0),
    Observaciones NVARCHAR(500) NULL,
    CONSTRAINT PK_PreferenciasCliente PRIMARY KEY (PreferenciaClienteId),
    CONSTRAINT UQ_PreferenciasCliente_Cliente UNIQUE (ClienteId),
    CONSTRAINT FK_PreferenciasCliente_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_PreferenciasCliente_Monedas FOREIGN KEY (MonedaPreferidaId) REFERENCES Configuracion.Monedas(MonedaId)
);
GO

CREATE TABLE Cliente.Pasajeros (
    PasajeroId BIGINT IDENTITY(1,1) NOT NULL,
    ClienteId BIGINT NULL,
    TipoDocumentoId INT NULL,
    NumeroDocumento NVARCHAR(40) NULL,
    Nombres NVARCHAR(120) NOT NULL,
    Apellidos NVARCHAR(120) NOT NULL,
    FechaNacimiento DATE NULL,
    NacionalidadPaisId INT NULL,
    RequerimientosEspeciales NVARCHAR(500) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Pasajeros_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Pasajeros PRIMARY KEY (PasajeroId),
    CONSTRAINT FK_Pasajeros_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_Pasajeros_TiposDocumento FOREIGN KEY (TipoDocumentoId) REFERENCES Configuracion.TiposDocumento(TipoDocumentoId),
    CONSTRAINT FK_Pasajeros_Paises FOREIGN KEY (NacionalidadPaisId) REFERENCES Catalogo.Paises(PaisId)
);
GO

CREATE TABLE Cliente.FavoritosDestino (
    ClienteId BIGINT NOT NULL,
    DestinoId BIGINT NOT NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_FavoritosDestino_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_FavoritosDestino PRIMARY KEY (ClienteId, DestinoId),
    CONSTRAINT FK_FavoritosDestino_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_FavoritosDestino_Destinos FOREIGN KEY (DestinoId) REFERENCES Catalogo.Destinos(DestinoId)
);
GO

CREATE TABLE Operacion.Proveedores (
    ProveedorId BIGINT IDENTITY(1,1) NOT NULL,
    TipoDocumentoId INT NULL,
    NumeroDocumento NVARCHAR(40) NULL,
    RazonSocial NVARCHAR(180) NOT NULL,
    NombreComercial NVARCHAR(180) NULL,
    Email NVARCHAR(255) NULL,
    Telefono NVARCHAR(30) NULL,
    CiudadId INT NULL,
    EstadoId INT NOT NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Proveedores_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Proveedores PRIMARY KEY (ProveedorId),
    CONSTRAINT FK_Proveedores_TiposDocumento FOREIGN KEY (TipoDocumentoId) REFERENCES Configuracion.TiposDocumento(TipoDocumentoId),
    CONSTRAINT FK_Proveedores_Ciudades FOREIGN KEY (CiudadId) REFERENCES Catalogo.Ciudades(CiudadId),
    CONSTRAINT FK_Proveedores_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Proveedores_Email CHECK (Email IS NULL OR Email LIKE '%_@_%._%')
);
GO

CREATE TABLE Operacion.ProveedorContactos (
    ProveedorContactoId BIGINT IDENTITY(1,1) NOT NULL,
    ProveedorId BIGINT NOT NULL,
    Nombres NVARCHAR(120) NOT NULL,
    Apellidos NVARCHAR(120) NULL,
    Cargo NVARCHAR(120) NULL,
    Email NVARCHAR(255) NULL,
    Telefono NVARCHAR(30) NULL,
    EsPrincipal BIT NOT NULL CONSTRAINT DF_ProveedorContactos_EsPrincipal DEFAULT (0),
    CONSTRAINT PK_ProveedorContactos PRIMARY KEY (ProveedorContactoId),
    CONSTRAINT FK_ProveedorContactos_Proveedores FOREIGN KEY (ProveedorId) REFERENCES Operacion.Proveedores(ProveedorId),
    CONSTRAINT CK_ProveedorContactos_Email CHECK (Email IS NULL OR Email LIKE '%_@_%._%')
);
GO

CREATE TABLE Operacion.ProveedorServicios (
    ProveedorServicioId BIGINT IDENTITY(1,1) NOT NULL,
    ProveedorId BIGINT NOT NULL,
    MonedaId INT NOT NULL,
    EstadoId INT NOT NULL,
    TipoServicio NVARCHAR(60) NOT NULL,
    Nombre NVARCHAR(160) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    CostoBase DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_ProveedorServicios PRIMARY KEY (ProveedorServicioId),
    CONSTRAINT FK_ProveedorServicios_Proveedores FOREIGN KEY (ProveedorId) REFERENCES Operacion.Proveedores(ProveedorId),
    CONSTRAINT FK_ProveedorServicios_Monedas FOREIGN KEY (MonedaId) REFERENCES Configuracion.Monedas(MonedaId),
    CONSTRAINT FK_ProveedorServicios_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_ProveedorServicios_Tipo CHECK (TipoServicio IN ('HOTEL','TRANSPORTE','GUIA','ENTRADA','ALIMENTACION','SEGURO','OTRO')),
    CONSTRAINT CK_ProveedorServicios_Costo CHECK (CostoBase >= 0)
);
GO

CREATE TABLE Operacion.ContratosProveedor (
    ContratoProveedorId BIGINT IDENTITY(1,1) NOT NULL,
    ProveedorId BIGINT NOT NULL,
    CodigoContrato NVARCHAR(80) NOT NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NULL,
    Condiciones NVARCHAR(MAX) NULL,
    EstadoId INT NOT NULL,
    CONSTRAINT PK_ContratosProveedor PRIMARY KEY (ContratoProveedorId),
    CONSTRAINT UQ_ContratosProveedor_Codigo UNIQUE (CodigoContrato),
    CONSTRAINT FK_ContratosProveedor_Proveedores FOREIGN KEY (ProveedorId) REFERENCES Operacion.Proveedores(ProveedorId),
    CONSTRAINT FK_ContratosProveedor_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_ContratosProveedor_Fechas CHECK (FechaFin IS NULL OR FechaFin >= FechaInicio)
);
GO

CREATE TABLE Operacion.Guias (
    GuiaId BIGINT IDENTITY(1,1) NOT NULL,
    ProveedorId BIGINT NULL,
    TipoDocumentoId INT NULL,
    NumeroDocumento NVARCHAR(40) NULL,
    Nombres NVARCHAR(120) NOT NULL,
    Apellidos NVARCHAR(120) NOT NULL,
    Telefono NVARCHAR(30) NULL,
    Email NVARCHAR(255) NULL,
    Idiomas NVARCHAR(250) NULL,
    EstadoId INT NOT NULL,
    CONSTRAINT PK_Guias PRIMARY KEY (GuiaId),
    CONSTRAINT FK_Guias_Proveedores FOREIGN KEY (ProveedorId) REFERENCES Operacion.Proveedores(ProveedorId),
    CONSTRAINT FK_Guias_TiposDocumento FOREIGN KEY (TipoDocumentoId) REFERENCES Configuracion.TiposDocumento(TipoDocumentoId),
    CONSTRAINT FK_Guias_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Guias_Email CHECK (Email IS NULL OR Email LIKE '%_@_%._%')
);
GO

CREATE TABLE Operacion.Transportes (
    TransporteId BIGINT IDENTITY(1,1) NOT NULL,
    ProveedorId BIGINT NULL,
    TipoTransporte NVARCHAR(50) NOT NULL,
    Placa NVARCHAR(30) NULL,
    Marca NVARCHAR(80) NULL,
    Modelo NVARCHAR(80) NULL,
    Capacidad INT NOT NULL,
    EstadoId INT NOT NULL,
    CONSTRAINT PK_Transportes PRIMARY KEY (TransporteId),
    CONSTRAINT FK_Transportes_Proveedores FOREIGN KEY (ProveedorId) REFERENCES Operacion.Proveedores(ProveedorId),
    CONSTRAINT FK_Transportes_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Transportes_Capacidad CHECK (Capacidad > 0)
);
GO

CREATE TABLE Operacion.Hoteles (
    HotelId BIGINT IDENTITY(1,1) NOT NULL,
    ProveedorId BIGINT NULL,
    CiudadId INT NOT NULL,
    Nombre NVARCHAR(180) NOT NULL,
    CategoriaEstrellas DECIMAL(2,1) NULL,
    Direccion NVARCHAR(250) NULL,
    Telefono NVARCHAR(30) NULL,
    Email NVARCHAR(255) NULL,
    EstadoId INT NOT NULL,
    CONSTRAINT PK_Hoteles PRIMARY KEY (HotelId),
    CONSTRAINT FK_Hoteles_Proveedores FOREIGN KEY (ProveedorId) REFERENCES Operacion.Proveedores(ProveedorId),
    CONSTRAINT FK_Hoteles_Ciudades FOREIGN KEY (CiudadId) REFERENCES Catalogo.Ciudades(CiudadId),
    CONSTRAINT FK_Hoteles_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Hoteles_Estrellas CHECK (CategoriaEstrellas IS NULL OR CategoriaEstrellas BETWEEN 1 AND 5),
    CONSTRAINT CK_Hoteles_Email CHECK (Email IS NULL OR Email LIKE '%_@_%._%')
);
GO

CREATE TABLE Operacion.Habitaciones (
    HabitacionId BIGINT IDENTITY(1,1) NOT NULL,
    HotelId BIGINT NOT NULL,
    MonedaId INT NOT NULL,
    TipoHabitacion NVARCHAR(80) NOT NULL,
    Capacidad INT NOT NULL,
    PrecioNoche DECIMAL(18,2) NOT NULL,
    EstadoId INT NOT NULL,
    CONSTRAINT PK_Habitaciones PRIMARY KEY (HabitacionId),
    CONSTRAINT FK_Habitaciones_Hoteles FOREIGN KEY (HotelId) REFERENCES Operacion.Hoteles(HotelId),
    CONSTRAINT FK_Habitaciones_Monedas FOREIGN KEY (MonedaId) REFERENCES Configuracion.Monedas(MonedaId),
    CONSTRAINT FK_Habitaciones_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Habitaciones_Capacidad CHECK (Capacidad > 0),
    CONSTRAINT CK_Habitaciones_Precio CHECK (PrecioNoche >= 0)
);
GO

CREATE TABLE Operacion.Salidas (
    SalidaId BIGINT IDENTITY(1,1) NOT NULL,
    PaqueteId BIGINT NOT NULL,
    EstadoId INT NOT NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    CupoTotal INT NOT NULL,
    CupoReservado INT NOT NULL CONSTRAINT DF_Salidas_CupoReservado DEFAULT (0),
    PuntoEncuentro NVARCHAR(250) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Salidas_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Salidas PRIMARY KEY (SalidaId),
    CONSTRAINT FK_Salidas_Paquetes FOREIGN KEY (PaqueteId) REFERENCES Catalogo.Paquetes(PaqueteId),
    CONSTRAINT FK_Salidas_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Salidas_Fechas CHECK (FechaFin >= FechaInicio),
    CONSTRAINT CK_Salidas_Cupos CHECK (CupoTotal >= 0 AND CupoReservado >= 0 AND CupoReservado <= CupoTotal)
);
GO

CREATE TABLE Operacion.CuposSalida (
    CupoSalidaId BIGINT IDENTITY(1,1) NOT NULL,
    SalidaId BIGINT NOT NULL,
    TipoCupo NVARCHAR(40) NOT NULL,
    CupoTotal INT NOT NULL,
    CupoReservado INT NOT NULL CONSTRAINT DF_CuposSalida_CupoReservado DEFAULT (0),
    CONSTRAINT PK_CuposSalida PRIMARY KEY (CupoSalidaId),
    CONSTRAINT FK_CuposSalida_Salidas FOREIGN KEY (SalidaId) REFERENCES Operacion.Salidas(SalidaId),
    CONSTRAINT UQ_CuposSalida_Salida_Tipo UNIQUE (SalidaId, TipoCupo),
    CONSTRAINT CK_CuposSalida_Cupos CHECK (CupoTotal >= 0 AND CupoReservado >= 0 AND CupoReservado <= CupoTotal)
);
GO

CREATE TABLE Operacion.BloqueosDisponibilidad (
    BloqueoDisponibilidadId BIGINT IDENTITY(1,1) NOT NULL,
    PaqueteId BIGINT NULL,
    SalidaId BIGINT NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    Motivo NVARCHAR(250) NOT NULL,
    CreadoPorUsuarioId BIGINT NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_BloqueosDisponibilidad_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_BloqueosDisponibilidad PRIMARY KEY (BloqueoDisponibilidadId),
    CONSTRAINT FK_BloqueosDisponibilidad_Paquetes FOREIGN KEY (PaqueteId) REFERENCES Catalogo.Paquetes(PaqueteId),
    CONSTRAINT FK_BloqueosDisponibilidad_Salidas FOREIGN KEY (SalidaId) REFERENCES Operacion.Salidas(SalidaId),
    CONSTRAINT FK_BloqueosDisponibilidad_Usuarios FOREIGN KEY (CreadoPorUsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT CK_BloqueosDisponibilidad_Fechas CHECK (FechaFin >= FechaInicio),
    CONSTRAINT CK_BloqueosDisponibilidad_Objeto CHECK (PaqueteId IS NOT NULL OR SalidaId IS NOT NULL)
);
GO

CREATE TABLE Operacion.AsignacionesOperacion (
    AsignacionOperacionId BIGINT IDENTITY(1,1) NOT NULL,
    SalidaId BIGINT NOT NULL,
    ProveedorServicioId BIGINT NULL,
    GuiaId BIGINT NULL,
    TransporteId BIGINT NULL,
    HotelId BIGINT NULL,
    HabitacionId BIGINT NULL,
    FechaAsignacion DATETIME2(0) NOT NULL CONSTRAINT DF_AsignacionesOperacion_Fecha DEFAULT SYSUTCDATETIME(),
    Observaciones NVARCHAR(500) NULL,
    CONSTRAINT PK_AsignacionesOperacion PRIMARY KEY (AsignacionOperacionId),
    CONSTRAINT FK_AsignacionesOperacion_Salidas FOREIGN KEY (SalidaId) REFERENCES Operacion.Salidas(SalidaId),
    CONSTRAINT FK_AsignacionesOperacion_ProveedorServicios FOREIGN KEY (ProveedorServicioId) REFERENCES Operacion.ProveedorServicios(ProveedorServicioId),
    CONSTRAINT FK_AsignacionesOperacion_Guias FOREIGN KEY (GuiaId) REFERENCES Operacion.Guias(GuiaId),
    CONSTRAINT FK_AsignacionesOperacion_Transportes FOREIGN KEY (TransporteId) REFERENCES Operacion.Transportes(TransporteId),
    CONSTRAINT FK_AsignacionesOperacion_Hoteles FOREIGN KEY (HotelId) REFERENCES Operacion.Hoteles(HotelId),
    CONSTRAINT FK_AsignacionesOperacion_Habitaciones FOREIGN KEY (HabitacionId) REFERENCES Operacion.Habitaciones(HabitacionId),
    CONSTRAINT CK_AsignacionesOperacion_Recurso CHECK (
        ProveedorServicioId IS NOT NULL OR GuiaId IS NOT NULL OR TransporteId IS NOT NULL OR HotelId IS NOT NULL OR HabitacionId IS NOT NULL
    )
);
GO

CREATE TABLE Operacion.ItinerarioOperativo (
    ItinerarioOperativoId BIGINT IDENTITY(1,1) NOT NULL,
    SalidaId BIGINT NOT NULL,
    DiaNumero INT NOT NULL,
    HoraInicio TIME(0) NULL,
    HoraFin TIME(0) NULL,
    Actividad NVARCHAR(180) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    CONSTRAINT PK_ItinerarioOperativo PRIMARY KEY (ItinerarioOperativoId),
    CONSTRAINT FK_ItinerarioOperativo_Salidas FOREIGN KEY (SalidaId) REFERENCES Operacion.Salidas(SalidaId),
    CONSTRAINT UQ_ItinerarioOperativo_Salida_Dia_Actividad UNIQUE (SalidaId, DiaNumero, Actividad),
    CONSTRAINT CK_ItinerarioOperativo_Dia CHECK (DiaNumero > 0),
    CONSTRAINT CK_ItinerarioOperativo_Horas CHECK (HoraFin IS NULL OR HoraInicio IS NULL OR HoraFin >= HoraInicio)
);
GO

CREATE TABLE Ventas.Reservas (
    ReservaId BIGINT IDENTITY(1,1) NOT NULL,
    CodigoReserva NVARCHAR(40) NOT NULL,
    ClienteId BIGINT NOT NULL,
    SalidaId BIGINT NOT NULL,
    EstadoId INT NOT NULL,
    MonedaId INT NOT NULL,
    FechaReserva DATETIME2(0) NOT NULL CONSTRAINT DF_Reservas_FechaReserva DEFAULT SYSUTCDATETIME(),
    FechaExpiracion DATETIME2(0) NULL,
    Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_Reservas_Subtotal DEFAULT (0),
    Impuestos DECIMAL(18,2) NOT NULL CONSTRAINT DF_Reservas_Impuestos DEFAULT (0),
    Descuento DECIMAL(18,2) NOT NULL CONSTRAINT DF_Reservas_Descuento DEFAULT (0),
    Total DECIMAL(18,2) NOT NULL CONSTRAINT DF_Reservas_Total DEFAULT (0),
    Observaciones NVARCHAR(500) NULL,
    FilaVersion ROWVERSION,
    CONSTRAINT PK_Reservas PRIMARY KEY (ReservaId),
    CONSTRAINT UQ_Reservas_Codigo UNIQUE (CodigoReserva),
    CONSTRAINT FK_Reservas_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_Reservas_Salidas FOREIGN KEY (SalidaId) REFERENCES Operacion.Salidas(SalidaId),
    CONSTRAINT FK_Reservas_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT FK_Reservas_Monedas FOREIGN KEY (MonedaId) REFERENCES Configuracion.Monedas(MonedaId),
    CONSTRAINT CK_Reservas_Montos CHECK (Subtotal >= 0 AND Impuestos >= 0 AND Descuento >= 0 AND Total >= 0),
    CONSTRAINT CK_Reservas_Expiracion CHECK (FechaExpiracion IS NULL OR FechaExpiracion >= FechaReserva)
);
GO

CREATE TABLE Ventas.ReservaDetalle (
    ReservaDetalleId BIGINT IDENTITY(1,1) NOT NULL,
    ReservaId BIGINT NOT NULL,
    PaqueteId BIGINT NULL,
    TarifaId BIGINT NULL,
    TipoDetalle NVARCHAR(40) NOT NULL,
    Descripcion NVARCHAR(250) NOT NULL,
    Cantidad INT NOT NULL,
    PrecioUnitario DECIMAL(18,2) NOT NULL,
    Importe DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_ReservaDetalle PRIMARY KEY (ReservaDetalleId),
    CONSTRAINT FK_ReservaDetalle_Reservas FOREIGN KEY (ReservaId) REFERENCES Ventas.Reservas(ReservaId),
    CONSTRAINT FK_ReservaDetalle_Paquetes FOREIGN KEY (PaqueteId) REFERENCES Catalogo.Paquetes(PaqueteId),
    CONSTRAINT FK_ReservaDetalle_Tarifas FOREIGN KEY (TarifaId) REFERENCES Catalogo.Tarifas(TarifaId),
    CONSTRAINT CK_ReservaDetalle_Tipo CHECK (TipoDetalle IN ('PAQUETE','EXTRA','DESCUENTO','IMPUESTO','SERVICIO')),
    CONSTRAINT CK_ReservaDetalle_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_ReservaDetalle_Montos CHECK (PrecioUnitario >= 0 AND Importe >= 0)
);
GO

CREATE TABLE Ventas.ReservaPasajeros (
    ReservaPasajeroId BIGINT IDENTITY(1,1) NOT NULL,
    ReservaId BIGINT NOT NULL,
    PasajeroId BIGINT NOT NULL,
    TipoPasajero NVARCHAR(30) NOT NULL,
    EsTitular BIT NOT NULL CONSTRAINT DF_ReservaPasajeros_EsTitular DEFAULT (0),
    CONSTRAINT PK_ReservaPasajeros PRIMARY KEY (ReservaPasajeroId),
    CONSTRAINT UQ_ReservaPasajeros_Reserva_Pasajero UNIQUE (ReservaId, PasajeroId),
    CONSTRAINT FK_ReservaPasajeros_Reservas FOREIGN KEY (ReservaId) REFERENCES Ventas.Reservas(ReservaId),
    CONSTRAINT FK_ReservaPasajeros_Pasajeros FOREIGN KEY (PasajeroId) REFERENCES Cliente.Pasajeros(PasajeroId),
    CONSTRAINT CK_ReservaPasajeros_Tipo CHECK (TipoPasajero IN ('ADULTO','NINO','INFANTE','ADULTO_MAYOR'))
);
GO

CREATE TABLE Ventas.ReservaHistorialEstados (
    ReservaHistorialEstadoId BIGINT IDENTITY(1,1) NOT NULL,
    ReservaId BIGINT NOT NULL,
    EstadoAnteriorId INT NULL,
    EstadoNuevoId INT NOT NULL,
    UsuarioId BIGINT NULL,
    Comentario NVARCHAR(500) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_ReservaHistorialEstados_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ReservaHistorialEstados PRIMARY KEY (ReservaHistorialEstadoId),
    CONSTRAINT FK_ReservaHistorialEstados_Reservas FOREIGN KEY (ReservaId) REFERENCES Ventas.Reservas(ReservaId),
    CONSTRAINT FK_ReservaHistorialEstados_EstadoAnterior FOREIGN KEY (EstadoAnteriorId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT FK_ReservaHistorialEstados_EstadoNuevo FOREIGN KEY (EstadoNuevoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT FK_ReservaHistorialEstados_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId)
);
GO

CREATE TABLE Pagos.MetodosPago (
    MetodoPagoId INT IDENTITY(1,1) NOT NULL,
    Codigo NVARCHAR(50) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    RequierePasarela BIT NOT NULL CONSTRAINT DF_MetodosPago_RequierePasarela DEFAULT (1),
    EsActivo BIT NOT NULL CONSTRAINT DF_MetodosPago_EsActivo DEFAULT (1),
    CONSTRAINT PK_MetodosPago PRIMARY KEY (MetodoPagoId),
    CONSTRAINT UQ_MetodosPago_Codigo UNIQUE (Codigo)
);
GO

CREATE TABLE Pagos.OrdenesPago (
    OrdenPagoId BIGINT IDENTITY(1,1) NOT NULL,
    ReservaId BIGINT NOT NULL,
    MonedaId INT NOT NULL,
    EstadoId INT NOT NULL,
    CodigoOrden NVARCHAR(60) NOT NULL,
    MontoTotal DECIMAL(18,2) NOT NULL,
    FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_OrdenesPago_FechaCreacion DEFAULT SYSUTCDATETIME(),
    FechaExpiracion DATETIME2(0) NULL,
    CONSTRAINT PK_OrdenesPago PRIMARY KEY (OrdenPagoId),
    CONSTRAINT UQ_OrdenesPago_Codigo UNIQUE (CodigoOrden),
    CONSTRAINT FK_OrdenesPago_Reservas FOREIGN KEY (ReservaId) REFERENCES Ventas.Reservas(ReservaId),
    CONSTRAINT FK_OrdenesPago_Monedas FOREIGN KEY (MonedaId) REFERENCES Configuracion.Monedas(MonedaId),
    CONSTRAINT FK_OrdenesPago_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_OrdenesPago_Monto CHECK (MontoTotal >= 0),
    CONSTRAINT CK_OrdenesPago_Expira CHECK (FechaExpiracion IS NULL OR FechaExpiracion >= FechaCreacion)
);
GO

CREATE TABLE Pagos.Pagos (
    PagoId BIGINT IDENTITY(1,1) NOT NULL,
    OrdenPagoId BIGINT NOT NULL,
    ReservaId BIGINT NOT NULL,
    MetodoPagoId INT NOT NULL,
    MonedaId INT NOT NULL,
    EstadoId INT NOT NULL,
    CodigoTransaccion NVARCHAR(120) NULL,
    Monto DECIMAL(18,2) NOT NULL,
    FechaPago DATETIME2(0) NULL,
    ReferenciaPasarela NVARCHAR(200) NULL,
    Observaciones NVARCHAR(500) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Pagos_CreadoEn DEFAULT SYSUTCDATETIME(),
    FilaVersion ROWVERSION,
    CONSTRAINT PK_Pagos PRIMARY KEY (PagoId),
    CONSTRAINT FK_Pagos_OrdenesPago FOREIGN KEY (OrdenPagoId) REFERENCES Pagos.OrdenesPago(OrdenPagoId),
    CONSTRAINT FK_Pagos_Reservas FOREIGN KEY (ReservaId) REFERENCES Ventas.Reservas(ReservaId),
    CONSTRAINT FK_Pagos_MetodosPago FOREIGN KEY (MetodoPagoId) REFERENCES Pagos.MetodosPago(MetodoPagoId),
    CONSTRAINT FK_Pagos_Monedas FOREIGN KEY (MonedaId) REFERENCES Configuracion.Monedas(MonedaId),
    CONSTRAINT FK_Pagos_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Pagos_Monto CHECK (Monto >= 0)
);
GO

CREATE TABLE Pagos.PagoIntentos (
    PagoIntentoId BIGINT IDENTITY(1,1) NOT NULL,
    OrdenPagoId BIGINT NOT NULL,
    PagoId BIGINT NULL,
    MetodoPagoId INT NOT NULL,
    Resultado NVARCHAR(40) NOT NULL,
    Mensaje NVARCHAR(500) NULL,
    PayloadRespuesta NVARCHAR(MAX) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_PagoIntentos_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_PagoIntentos PRIMARY KEY (PagoIntentoId),
    CONSTRAINT FK_PagoIntentos_OrdenesPago FOREIGN KEY (OrdenPagoId) REFERENCES Pagos.OrdenesPago(OrdenPagoId),
    CONSTRAINT FK_PagoIntentos_Pagos FOREIGN KEY (PagoId) REFERENCES Pagos.Pagos(PagoId),
    CONSTRAINT FK_PagoIntentos_MetodosPago FOREIGN KEY (MetodoPagoId) REFERENCES Pagos.MetodosPago(MetodoPagoId),
    CONSTRAINT CK_PagoIntentos_Resultado CHECK (Resultado IN ('INICIADO','APROBADO','RECHAZADO','ERROR','EXPIRADO')),
    CONSTRAINT CK_PagoIntentos_PayloadJson CHECK (PayloadRespuesta IS NULL OR ISJSON(PayloadRespuesta) = 1)
);
GO

CREATE TABLE Pagos.Reembolsos (
    ReembolsoId BIGINT IDENTITY(1,1) NOT NULL,
    PagoId BIGINT NOT NULL,
    MonedaId INT NOT NULL,
    EstadoId INT NOT NULL,
    CodigoReembolso NVARCHAR(80) NOT NULL,
    Monto DECIMAL(18,2) NOT NULL,
    Motivo NVARCHAR(500) NOT NULL,
    FechaSolicitud DATETIME2(0) NOT NULL CONSTRAINT DF_Reembolsos_FechaSolicitud DEFAULT SYSUTCDATETIME(),
    FechaProcesado DATETIME2(0) NULL,
    CONSTRAINT PK_Reembolsos PRIMARY KEY (ReembolsoId),
    CONSTRAINT UQ_Reembolsos_Codigo UNIQUE (CodigoReembolso),
    CONSTRAINT FK_Reembolsos_Pagos FOREIGN KEY (PagoId) REFERENCES Pagos.Pagos(PagoId),
    CONSTRAINT FK_Reembolsos_Monedas FOREIGN KEY (MonedaId) REFERENCES Configuracion.Monedas(MonedaId),
    CONSTRAINT FK_Reembolsos_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Reembolsos_Monto CHECK (Monto > 0),
    CONSTRAINT CK_Reembolsos_Fechas CHECK (FechaProcesado IS NULL OR FechaProcesado >= FechaSolicitud)
);
GO

CREATE TABLE Ventas.Compras (
    CompraId BIGINT IDENTITY(1,1) NOT NULL,
    ReservaId BIGINT NOT NULL,
    ClienteId BIGINT NOT NULL,
    PagoId BIGINT NOT NULL,
    EstadoId INT NOT NULL,
    CodigoCompra NVARCHAR(50) NOT NULL,
    FechaCompra DATETIME2(0) NOT NULL CONSTRAINT DF_Compras_FechaCompra DEFAULT SYSUTCDATETIME(),
    TotalPagado DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_Compras PRIMARY KEY (CompraId),
    CONSTRAINT UQ_Compras_Codigo UNIQUE (CodigoCompra),
    CONSTRAINT UQ_Compras_Reserva UNIQUE (ReservaId),
    CONSTRAINT FK_Compras_Reservas FOREIGN KEY (ReservaId) REFERENCES Ventas.Reservas(ReservaId),
    CONSTRAINT FK_Compras_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_Compras_Pagos FOREIGN KEY (PagoId) REFERENCES Pagos.Pagos(PagoId),
    CONSTRAINT FK_Compras_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Compras_Total CHECK (TotalPagado >= 0)
);
GO

CREATE TABLE Ventas.Comprobantes (
    ComprobanteId BIGINT IDENTITY(1,1) NOT NULL,
    CompraId BIGINT NOT NULL,
    ClienteId BIGINT NOT NULL,
    MonedaId INT NOT NULL,
    EstadoId INT NOT NULL,
    TipoComprobante NVARCHAR(30) NOT NULL,
    Serie NVARCHAR(10) NOT NULL,
    Numero NVARCHAR(20) NOT NULL,
    FechaEmision DATETIME2(0) NOT NULL CONSTRAINT DF_Comprobantes_FechaEmision DEFAULT SYSUTCDATETIME(),
    Subtotal DECIMAL(18,2) NOT NULL,
    Impuestos DECIMAL(18,2) NOT NULL,
    Total DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_Comprobantes PRIMARY KEY (ComprobanteId),
    CONSTRAINT UQ_Comprobantes_Serie_Numero UNIQUE (Serie, Numero),
    CONSTRAINT FK_Comprobantes_Compras FOREIGN KEY (CompraId) REFERENCES Ventas.Compras(CompraId),
    CONSTRAINT FK_Comprobantes_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_Comprobantes_Monedas FOREIGN KEY (MonedaId) REFERENCES Configuracion.Monedas(MonedaId),
    CONSTRAINT FK_Comprobantes_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Comprobantes_Tipo CHECK (TipoComprobante IN ('BOLETA','FACTURA','NOTA_CREDITO')),
    CONSTRAINT CK_Comprobantes_Montos CHECK (Subtotal >= 0 AND Impuestos >= 0 AND Total >= 0)
);
GO

CREATE TABLE Ventas.ComprobanteDetalle (
    ComprobanteDetalleId BIGINT IDENTITY(1,1) NOT NULL,
    ComprobanteId BIGINT NOT NULL,
    Descripcion NVARCHAR(250) NOT NULL,
    Cantidad INT NOT NULL,
    PrecioUnitario DECIMAL(18,2) NOT NULL,
    Importe DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_ComprobanteDetalle PRIMARY KEY (ComprobanteDetalleId),
    CONSTRAINT FK_ComprobanteDetalle_Comprobantes FOREIGN KEY (ComprobanteId) REFERENCES Ventas.Comprobantes(ComprobanteId),
    CONSTRAINT CK_ComprobanteDetalle_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_ComprobanteDetalle_Montos CHECK (PrecioUnitario >= 0 AND Importe >= 0)
);
GO

CREATE TABLE Ventas.TicketsViaje (
    TicketViajeId BIGINT IDENTITY(1,1) NOT NULL,
    CompraId BIGINT NOT NULL,
    ReservaId BIGINT NOT NULL,
    PasajeroId BIGINT NOT NULL,
    SalidaId BIGINT NOT NULL,
    EstadoId INT NOT NULL,
    CodigoTicket NVARCHAR(80) NOT NULL,
    EmitidoEn DATETIME2(0) NOT NULL CONSTRAINT DF_TicketsViaje_EmitidoEn DEFAULT SYSUTCDATETIME(),
    UrlDocumento NVARCHAR(1000) NULL,
    CONSTRAINT PK_TicketsViaje PRIMARY KEY (TicketViajeId),
    CONSTRAINT UQ_TicketsViaje_Codigo UNIQUE (CodigoTicket),
    CONSTRAINT FK_TicketsViaje_Compras FOREIGN KEY (CompraId) REFERENCES Ventas.Compras(CompraId),
    CONSTRAINT FK_TicketsViaje_Reservas FOREIGN KEY (ReservaId) REFERENCES Ventas.Reservas(ReservaId),
    CONSTRAINT FK_TicketsViaje_Pasajeros FOREIGN KEY (PasajeroId) REFERENCES Cliente.Pasajeros(PasajeroId),
    CONSTRAINT FK_TicketsViaje_Salidas FOREIGN KEY (SalidaId) REFERENCES Operacion.Salidas(SalidaId),
    CONSTRAINT FK_TicketsViaje_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId)
);
GO

CREATE TABLE Ventas.Vouchers (
    VoucherId BIGINT IDENTITY(1,1) NOT NULL,
    CompraId BIGINT NOT NULL,
    ReservaId BIGINT NOT NULL,
    EstadoId INT NOT NULL,
    CodigoVoucher NVARCHAR(80) NOT NULL,
    UrlDocumento NVARCHAR(1000) NULL,
    EmitidoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Vouchers_EmitidoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Vouchers PRIMARY KEY (VoucherId),
    CONSTRAINT UQ_Vouchers_Codigo UNIQUE (CodigoVoucher),
    CONSTRAINT FK_Vouchers_Compras FOREIGN KEY (CompraId) REFERENCES Ventas.Compras(CompraId),
    CONSTRAINT FK_Vouchers_Reservas FOREIGN KEY (ReservaId) REFERENCES Ventas.Reservas(ReservaId),
    CONSTRAINT FK_Vouchers_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId)
);
GO

CREATE TABLE Ventas.Resenas (
    ResenaId BIGINT IDENTITY(1,1) NOT NULL,
    ClienteId BIGINT NOT NULL,
    ReservaId BIGINT NULL,
    DestinoId BIGINT NOT NULL,
    EstadoModeracionId INT NOT NULL,
    Calificacion TINYINT NOT NULL,
    Titulo NVARCHAR(160) NULL,
    Comentario NVARCHAR(2000) NOT NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Resenas_CreadoEn DEFAULT SYSUTCDATETIME(),
    ModeradoEn DATETIME2(0) NULL,
    ModeradoPorUsuarioId BIGINT NULL,
    CONSTRAINT PK_Resenas PRIMARY KEY (ResenaId),
    CONSTRAINT FK_Resenas_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_Resenas_Reservas FOREIGN KEY (ReservaId) REFERENCES Ventas.Reservas(ReservaId),
    CONSTRAINT FK_Resenas_Destinos FOREIGN KEY (DestinoId) REFERENCES Catalogo.Destinos(DestinoId),
    CONSTRAINT FK_Resenas_Estados FOREIGN KEY (EstadoModeracionId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT FK_Resenas_Moderador FOREIGN KEY (ModeradoPorUsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT CK_Resenas_Calificacion CHECK (Calificacion BETWEEN 1 AND 5),
    CONSTRAINT CK_Resenas_ModeracionFecha CHECK (ModeradoEn IS NULL OR ModeradoEn >= CreadoEn)
);
GO

CREATE TABLE Ventas.ResenaRespuestas (
    ResenaRespuestaId BIGINT IDENTITY(1,1) NOT NULL,
    ResenaId BIGINT NOT NULL,
    UsuarioId BIGINT NOT NULL,
    Respuesta NVARCHAR(2000) NOT NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_ResenaRespuestas_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ResenaRespuestas PRIMARY KEY (ResenaRespuestaId),
    CONSTRAINT FK_ResenaRespuestas_Resenas FOREIGN KEY (ResenaId) REFERENCES Ventas.Resenas(ResenaId),
    CONSTRAINT FK_ResenaRespuestas_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId)
);
GO

CREATE TABLE Ventas.ResenaReportes (
    ResenaReporteId BIGINT IDENTITY(1,1) NOT NULL,
    ResenaId BIGINT NOT NULL,
    UsuarioId BIGINT NULL,
    Motivo NVARCHAR(250) NOT NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_ResenaReportes_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ResenaReportes PRIMARY KEY (ResenaReporteId),
    CONSTRAINT FK_ResenaReportes_Resenas FOREIGN KEY (ResenaId) REFERENCES Ventas.Resenas(ResenaId),
    CONSTRAINT FK_ResenaReportes_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId)
);
GO

CREATE TABLE Marketing.CampanasPromocionales (
    CampanaPromocionalId BIGINT IDENTITY(1,1) NOT NULL,
    EstadoId INT NOT NULL,
    Codigo NVARCHAR(60) NOT NULL,
    Nombre NVARCHAR(160) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    FechaInicio DATETIME2(0) NOT NULL,
    FechaFin DATETIME2(0) NULL,
    Presupuesto DECIMAL(18,2) NULL,
    CONSTRAINT PK_CampanasPromocionales PRIMARY KEY (CampanaPromocionalId),
    CONSTRAINT UQ_CampanasPromocionales_Codigo UNIQUE (Codigo),
    CONSTRAINT FK_CampanasPromocionales_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_CampanasPromocionales_Fechas CHECK (FechaFin IS NULL OR FechaFin >= FechaInicio),
    CONSTRAINT CK_CampanasPromocionales_Presupuesto CHECK (Presupuesto IS NULL OR Presupuesto >= 0)
);
GO

CREATE TABLE Marketing.Descuentos (
    DescuentoId BIGINT IDENTITY(1,1) NOT NULL,
    CampanaPromocionalId BIGINT NULL,
    Codigo NVARCHAR(60) NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    TipoDescuento NVARCHAR(30) NOT NULL,
    Valor DECIMAL(18,2) NOT NULL,
    MontoMaximo DECIMAL(18,2) NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_Descuentos_EsActivo DEFAULT (1),
    CONSTRAINT PK_Descuentos PRIMARY KEY (DescuentoId),
    CONSTRAINT UQ_Descuentos_Codigo UNIQUE (Codigo),
    CONSTRAINT FK_Descuentos_Campanas FOREIGN KEY (CampanaPromocionalId) REFERENCES Marketing.CampanasPromocionales(CampanaPromocionalId),
    CONSTRAINT CK_Descuentos_Tipo CHECK (TipoDescuento IN ('PORCENTAJE','MONTO_FIJO')),
    CONSTRAINT CK_Descuentos_Valor CHECK (Valor > 0),
    CONSTRAINT CK_Descuentos_Porcentaje CHECK (TipoDescuento <> 'PORCENTAJE' OR Valor <= 100),
    CONSTRAINT CK_Descuentos_MontoMaximo CHECK (MontoMaximo IS NULL OR MontoMaximo >= 0)
);
GO

CREATE TABLE Marketing.Cupones (
    CuponId BIGINT IDENTITY(1,1) NOT NULL,
    DescuentoId BIGINT NOT NULL,
    Codigo NVARCHAR(80) NOT NULL,
    UsoMaximo INT NULL,
    UsoActual INT NOT NULL CONSTRAINT DF_Cupones_UsoActual DEFAULT (0),
    FechaInicio DATETIME2(0) NOT NULL,
    FechaFin DATETIME2(0) NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_Cupones_EsActivo DEFAULT (1),
    CONSTRAINT PK_Cupones PRIMARY KEY (CuponId),
    CONSTRAINT UQ_Cupones_Codigo UNIQUE (Codigo),
    CONSTRAINT FK_Cupones_Descuentos FOREIGN KEY (DescuentoId) REFERENCES Marketing.Descuentos(DescuentoId),
    CONSTRAINT CK_Cupones_Usos CHECK (UsoActual >= 0 AND (UsoMaximo IS NULL OR UsoMaximo >= UsoActual)),
    CONSTRAINT CK_Cupones_Fechas CHECK (FechaFin IS NULL OR FechaFin >= FechaInicio)
);
GO

CREATE TABLE Marketing.PaquetePromociones (
    PaqueteId BIGINT NOT NULL,
    DescuentoId BIGINT NOT NULL,
    CONSTRAINT PK_PaquetePromociones PRIMARY KEY (PaqueteId, DescuentoId),
    CONSTRAINT FK_PaquetePromociones_Paquetes FOREIGN KEY (PaqueteId) REFERENCES Catalogo.Paquetes(PaqueteId),
    CONSTRAINT FK_PaquetePromociones_Descuentos FOREIGN KEY (DescuentoId) REFERENCES Marketing.Descuentos(DescuentoId)
);
GO

CREATE TABLE Marketing.LandingPages (
    LandingPageId BIGINT IDENTITY(1,1) NOT NULL,
    CampanaPromocionalId BIGINT NULL,
    Slug NVARCHAR(160) NOT NULL,
    Titulo NVARCHAR(180) NOT NULL,
    Oferta NVARCHAR(300) NULL,
    EstadoId INT NOT NULL,
    PublicadaEn DATETIME2(0) NULL,
    CONSTRAINT PK_LandingPages PRIMARY KEY (LandingPageId),
    CONSTRAINT UQ_LandingPages_Slug UNIQUE (Slug),
    CONSTRAINT FK_LandingPages_Campanas FOREIGN KEY (CampanaPromocionalId) REFERENCES Marketing.CampanasPromocionales(CampanaPromocionalId),
    CONSTRAINT FK_LandingPages_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId)
);
GO

CREATE TABLE Marketing.Leads (
    LeadId BIGINT IDENTITY(1,1) NOT NULL,
    LandingPageId BIGINT NULL,
    EstadoId INT NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    Nombres NVARCHAR(120) NULL,
    Telefono NVARCHAR(30) NULL,
    Origen NVARCHAR(120) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Leads_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Leads PRIMARY KEY (LeadId),
    CONSTRAINT FK_Leads_LandingPages FOREIGN KEY (LandingPageId) REFERENCES Marketing.LandingPages(LandingPageId),
    CONSTRAINT FK_Leads_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Leads_Email CHECK (Email LIKE '%_@_%._%')
);
GO

CREATE TABLE Marketing.LeadConsentimientos (
    LeadConsentimientoId BIGINT IDENTITY(1,1) NOT NULL,
    LeadId BIGINT NOT NULL,
    TipoConsentimiento NVARCHAR(40) NOT NULL,
    Aceptado BIT NOT NULL,
    FechaConsentimiento DATETIME2(0) NOT NULL CONSTRAINT DF_LeadConsentimientos_Fecha DEFAULT SYSUTCDATETIME(),
    Ip NVARCHAR(45) NULL,
    CONSTRAINT PK_LeadConsentimientos PRIMARY KEY (LeadConsentimientoId),
    CONSTRAINT FK_LeadConsentimientos_Leads FOREIGN KEY (LeadId) REFERENCES Marketing.Leads(LeadId),
    CONSTRAINT CK_LeadConsentimientos_Tipo CHECK (TipoConsentimiento IN ('PRIVACIDAD','MARKETING','COOKIES'))
);
GO

CREATE TABLE Marketing.LeadActividades (
    LeadActividadId BIGINT IDENTITY(1,1) NOT NULL,
    LeadId BIGINT NOT NULL,
    TipoActividad NVARCHAR(50) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    DatosJson NVARCHAR(MAX) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_LeadActividades_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_LeadActividades PRIMARY KEY (LeadActividadId),
    CONSTRAINT FK_LeadActividades_Leads FOREIGN KEY (LeadId) REFERENCES Marketing.Leads(LeadId),
    CONSTRAINT CK_LeadActividades_DatosJson CHECK (DatosJson IS NULL OR ISJSON(DatosJson) = 1)
);
GO

CREATE TABLE Marketing.CampanaLeads (
    CampanaPromocionalId BIGINT NOT NULL,
    LeadId BIGINT NOT NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_CampanaLeads_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_CampanaLeads PRIMARY KEY (CampanaPromocionalId, LeadId),
    CONSTRAINT FK_CampanaLeads_Campanas FOREIGN KEY (CampanaPromocionalId) REFERENCES Marketing.CampanasPromocionales(CampanaPromocionalId),
    CONSTRAINT FK_CampanaLeads_Leads FOREIGN KEY (LeadId) REFERENCES Marketing.Leads(LeadId)
);
GO

CREATE TABLE Soporte.MensajesContacto (
    MensajeContactoId BIGINT IDENTITY(1,1) NOT NULL,
    ClienteId BIGINT NULL,
    UsuarioId BIGINT NULL,
    EstadoId INT NOT NULL,
    Nombre NVARCHAR(160) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    Asunto NVARCHAR(120) NOT NULL,
    Mensaje NVARCHAR(MAX) NOT NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_MensajesContacto_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_MensajesContacto PRIMARY KEY (MensajeContactoId),
    CONSTRAINT FK_MensajesContacto_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_MensajesContacto_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT FK_MensajesContacto_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_MensajesContacto_Email CHECK (Email LIKE '%_@_%._%')
);
GO

CREATE TABLE Soporte.CategoriasSoporte (
    CategoriaSoporteId INT IDENTITY(1,1) NOT NULL,
    Codigo NVARCHAR(50) NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_CategoriasSoporte_EsActivo DEFAULT (1),
    CONSTRAINT PK_CategoriasSoporte PRIMARY KEY (CategoriaSoporteId),
    CONSTRAINT UQ_CategoriasSoporte_Codigo UNIQUE (Codigo)
);
GO

CREATE TABLE Soporte.TicketsSoporte (
    TicketSoporteId BIGINT IDENTITY(1,1) NOT NULL,
    ClienteId BIGINT NULL,
    ReservaId BIGINT NULL,
    CategoriaSoporteId INT NOT NULL,
    EstadoId INT NOT NULL,
    UsuarioAsignadoId BIGINT NULL,
    CodigoTicket NVARCHAR(60) NOT NULL,
    Prioridad NVARCHAR(20) NOT NULL,
    Asunto NVARCHAR(180) NOT NULL,
    Descripcion NVARCHAR(MAX) NOT NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_TicketsSoporte_CreadoEn DEFAULT SYSUTCDATETIME(),
    CerradoEn DATETIME2(0) NULL,
    CONSTRAINT PK_TicketsSoporte PRIMARY KEY (TicketSoporteId),
    CONSTRAINT UQ_TicketsSoporte_Codigo UNIQUE (CodigoTicket),
    CONSTRAINT FK_TicketsSoporte_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_TicketsSoporte_Reservas FOREIGN KEY (ReservaId) REFERENCES Ventas.Reservas(ReservaId),
    CONSTRAINT FK_TicketsSoporte_Categorias FOREIGN KEY (CategoriaSoporteId) REFERENCES Soporte.CategoriasSoporte(CategoriaSoporteId),
    CONSTRAINT FK_TicketsSoporte_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT FK_TicketsSoporte_UsuarioAsignado FOREIGN KEY (UsuarioAsignadoId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT CK_TicketsSoporte_Prioridad CHECK (Prioridad IN ('BAJA','MEDIA','ALTA','CRITICA')),
    CONSTRAINT CK_TicketsSoporte_Fechas CHECK (CerradoEn IS NULL OR CerradoEn >= CreadoEn)
);
GO

CREATE TABLE Soporte.TicketEventos (
    TicketEventoId BIGINT IDENTITY(1,1) NOT NULL,
    TicketSoporteId BIGINT NOT NULL,
    UsuarioId BIGINT NULL,
    TipoEvento NVARCHAR(50) NOT NULL,
    Comentario NVARCHAR(MAX) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_TicketEventos_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TicketEventos PRIMARY KEY (TicketEventoId),
    CONSTRAINT FK_TicketEventos_Tickets FOREIGN KEY (TicketSoporteId) REFERENCES Soporte.TicketsSoporte(TicketSoporteId),
    CONSTRAINT FK_TicketEventos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT CK_TicketEventos_Tipo CHECK (TipoEvento IN ('CREADO','COMENTARIO','ASIGNADO','CAMBIO_ESTADO','CERRADO'))
);
GO

CREATE TABLE Soporte.CanalesNotificacion (
    CanalNotificacionId INT IDENTITY(1,1) NOT NULL,
    Codigo NVARCHAR(50) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_CanalesNotificacion_EsActivo DEFAULT (1),
    CONSTRAINT PK_CanalesNotificacion PRIMARY KEY (CanalNotificacionId),
    CONSTRAINT UQ_CanalesNotificacion_Codigo UNIQUE (Codigo)
);
GO

CREATE TABLE Soporte.PlantillasNotificacion (
    PlantillaNotificacionId BIGINT IDENTITY(1,1) NOT NULL,
    CanalNotificacionId INT NOT NULL,
    Codigo NVARCHAR(80) NOT NULL,
    Asunto NVARCHAR(180) NULL,
    Cuerpo NVARCHAR(MAX) NOT NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_PlantillasNotificacion_EsActivo DEFAULT (1),
    CONSTRAINT PK_PlantillasNotificacion PRIMARY KEY (PlantillaNotificacionId),
    CONSTRAINT UQ_PlantillasNotificacion_Codigo UNIQUE (Codigo),
    CONSTRAINT FK_PlantillasNotificacion_Canales FOREIGN KEY (CanalNotificacionId) REFERENCES Soporte.CanalesNotificacion(CanalNotificacionId)
);
GO

CREATE TABLE Soporte.PreferenciasNotificacion (
    PreferenciaNotificacionId BIGINT IDENTITY(1,1) NOT NULL,
    ClienteId BIGINT NOT NULL,
    CanalNotificacionId INT NOT NULL,
    Habilitado BIT NOT NULL CONSTRAINT DF_PreferenciasNotificacion_Habilitado DEFAULT (1),
    CONSTRAINT PK_PreferenciasNotificacion PRIMARY KEY (PreferenciaNotificacionId),
    CONSTRAINT UQ_PreferenciasNotificacion_Cliente_Canal UNIQUE (ClienteId, CanalNotificacionId),
    CONSTRAINT FK_PreferenciasNotificacion_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_PreferenciasNotificacion_Canales FOREIGN KEY (CanalNotificacionId) REFERENCES Soporte.CanalesNotificacion(CanalNotificacionId)
);
GO

CREATE TABLE Soporte.Notificaciones (
    NotificacionId BIGINT IDENTITY(1,1) NOT NULL,
    ClienteId BIGINT NULL,
    UsuarioId BIGINT NULL,
    PlantillaNotificacionId BIGINT NULL,
    CanalNotificacionId INT NOT NULL,
    EstadoId INT NOT NULL,
    Destinatario NVARCHAR(255) NOT NULL,
    Asunto NVARCHAR(180) NULL,
    Cuerpo NVARCHAR(MAX) NOT NULL,
    EnviadoEn DATETIME2(0) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_Notificaciones_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Notificaciones PRIMARY KEY (NotificacionId),
    CONSTRAINT FK_Notificaciones_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT FK_Notificaciones_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT FK_Notificaciones_Plantillas FOREIGN KEY (PlantillaNotificacionId) REFERENCES Soporte.PlantillasNotificacion(PlantillaNotificacionId),
    CONSTRAINT FK_Notificaciones_Canales FOREIGN KEY (CanalNotificacionId) REFERENCES Soporte.CanalesNotificacion(CanalNotificacionId),
    CONSTRAINT FK_Notificaciones_Estados FOREIGN KEY (EstadoId) REFERENCES Configuracion.Estados(EstadoId),
    CONSTRAINT CK_Notificaciones_Fechas CHECK (EnviadoEn IS NULL OR EnviadoEn >= CreadoEn)
);
GO

CREATE TABLE Reportes.KpiDefiniciones (
    KpiDefinicionId INT IDENTITY(1,1) NOT NULL,
    Codigo NVARCHAR(80) NOT NULL,
    Nombre NVARCHAR(160) NOT NULL,
    Descripcion NVARCHAR(500) NULL,
    Formula NVARCHAR(MAX) NULL,
    EsActivo BIT NOT NULL CONSTRAINT DF_KpiDefiniciones_EsActivo DEFAULT (1),
    CONSTRAINT PK_KpiDefiniciones PRIMARY KEY (KpiDefinicionId),
    CONSTRAINT UQ_KpiDefiniciones_Codigo UNIQUE (Codigo)
);
GO

CREATE TABLE Reportes.EventosAnalitica (
    EventoAnaliticaId BIGINT IDENTITY(1,1) NOT NULL,
    UsuarioId BIGINT NULL,
    ClienteId BIGINT NULL,
    SesionId NVARCHAR(120) NULL,
    TipoEvento NVARCHAR(80) NOT NULL,
    Pagina NVARCHAR(250) NULL,
    RecursoId NVARCHAR(80) NULL,
    DatosJson NVARCHAR(MAX) NULL,
    Ip NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_EventosAnalitica_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_EventosAnalitica PRIMARY KEY (EventoAnaliticaId),
    CONSTRAINT FK_EventosAnalitica_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT FK_EventosAnalitica_Clientes FOREIGN KEY (ClienteId) REFERENCES Cliente.Clientes(ClienteId),
    CONSTRAINT CK_EventosAnalitica_DatosJson CHECK (DatosJson IS NULL OR ISJSON(DatosJson) = 1)
);
GO

CREATE TABLE Reportes.MetricasDiarias (
    MetricaDiariaId BIGINT IDENTITY(1,1) NOT NULL,
    KpiDefinicionId INT NULL,
    Fecha DATE NOT NULL,
    NombreMetrica NVARCHAR(120) NOT NULL,
    DimensionNombre NVARCHAR(120) NULL,
    DimensionValor NVARCHAR(180) NULL,
    ValorDecimal DECIMAL(18,4) NOT NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_MetricasDiarias_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_MetricasDiarias PRIMARY KEY (MetricaDiariaId),
    CONSTRAINT FK_MetricasDiarias_Kpi FOREIGN KEY (KpiDefinicionId) REFERENCES Reportes.KpiDefiniciones(KpiDefinicionId),
    CONSTRAINT UQ_MetricasDiarias_Fecha_Metrica_Dimension UNIQUE (Fecha, NombreMetrica, DimensionNombre, DimensionValor)
);
GO

CREATE TABLE Reportes.DashboardSnapshots (
    DashboardSnapshotId BIGINT IDENTITY(1,1) NOT NULL,
    CodigoDashboard NVARCHAR(80) NOT NULL,
    Titulo NVARCHAR(160) NOT NULL,
    DatosJson NVARCHAR(MAX) NOT NULL,
    GeneradoPorUsuarioId BIGINT NULL,
    GeneradoEn DATETIME2(0) NOT NULL CONSTRAINT DF_DashboardSnapshots_GeneradoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_DashboardSnapshots PRIMARY KEY (DashboardSnapshotId),
    CONSTRAINT FK_DashboardSnapshots_Usuarios FOREIGN KEY (GeneradoPorUsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT CK_DashboardSnapshots_DatosJson CHECK (ISJSON(DatosJson) = 1)
);
GO

CREATE TABLE Auditoria.AuditoriaCambios (
    AuditoriaCambioId BIGINT IDENTITY(1,1) NOT NULL,
    UsuarioId BIGINT NULL,
    EsquemaNombre NVARCHAR(80) NOT NULL,
    TablaNombre NVARCHAR(120) NOT NULL,
    ClavePrimaria NVARCHAR(120) NOT NULL,
    Operacion NVARCHAR(20) NOT NULL,
    DatosAntes NVARCHAR(MAX) NULL,
    DatosDespues NVARCHAR(MAX) NULL,
    Ip NVARCHAR(45) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_AuditoriaCambios_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_AuditoriaCambios PRIMARY KEY (AuditoriaCambioId),
    CONSTRAINT FK_AuditoriaCambios_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT CK_AuditoriaCambios_Operacion CHECK (Operacion IN ('INSERT','UPDATE','DELETE')),
    CONSTRAINT CK_AuditoriaCambios_DatosAntes CHECK (DatosAntes IS NULL OR ISJSON(DatosAntes) = 1),
    CONSTRAINT CK_AuditoriaCambios_DatosDespues CHECK (DatosDespues IS NULL OR ISJSON(DatosDespues) = 1)
);
GO

CREATE TABLE Auditoria.AuditoriaAccesos (
    AuditoriaAccesoId BIGINT IDENTITY(1,1) NOT NULL,
    UsuarioId BIGINT NULL,
    EmailIntentado NVARCHAR(255) NULL,
    Resultado NVARCHAR(40) NOT NULL,
    Ip NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_AuditoriaAccesos_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_AuditoriaAccesos PRIMARY KEY (AuditoriaAccesoId),
    CONSTRAINT FK_AuditoriaAccesos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT CK_AuditoriaAccesos_Resultado CHECK (Resultado IN ('EXITOSO','FALLIDO','BLOQUEADO','LOGOUT','REFRESH'))
);
GO

CREATE TABLE Auditoria.ErroresAplicacion (
    ErrorAplicacionId BIGINT IDENTITY(1,1) NOT NULL,
    UsuarioId BIGINT NULL,
    Nivel NVARCHAR(20) NOT NULL,
    Origen NVARCHAR(160) NOT NULL,
    Mensaje NVARCHAR(MAX) NOT NULL,
    StackTrace NVARCHAR(MAX) NULL,
    DatosJson NVARCHAR(MAX) NULL,
    CreadoEn DATETIME2(0) NOT NULL CONSTRAINT DF_ErroresAplicacion_CreadoEn DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ErroresAplicacion PRIMARY KEY (ErrorAplicacionId),
    CONSTRAINT FK_ErroresAplicacion_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Seguridad.Usuarios(UsuarioId),
    CONSTRAINT CK_ErroresAplicacion_Nivel CHECK (Nivel IN ('INFO','WARN','ERROR','CRITICAL')),
    CONSTRAINT CK_ErroresAplicacion_DatosJson CHECK (DatosJson IS NULL OR ISJSON(DatosJson) = 1)
);
GO

CREATE INDEX IX_Usuarios_EstadoId ON Seguridad.Usuarios(EstadoId);
CREATE INDEX IX_UsuarioRoles_RolId ON Seguridad.UsuarioRoles(RolId);
CREATE INDEX IX_RefreshTokens_UsuarioId ON Seguridad.RefreshTokens(UsuarioId);
CREATE INDEX IX_SesionesUsuario_UsuarioId_IniciadaEn ON Seguridad.SesionesUsuario(UsuarioId, IniciadaEn DESC);
GO

CREATE UNIQUE INDEX UX_Monedas_Base ON Configuracion.Monedas(EsMonedaBase) WHERE EsMonedaBase = 1;
CREATE INDEX IX_Estados_Grupo ON Configuracion.Estados(Grupo);
GO

CREATE INDEX IX_Regiones_PaisId ON Catalogo.Regiones(PaisId);
CREATE INDEX IX_Ciudades_RegionId ON Catalogo.Ciudades(RegionId);
CREATE INDEX IX_Destinos_CiudadId ON Catalogo.Destinos(CiudadId);
CREATE INDEX IX_Destinos_EstadoId ON Catalogo.Destinos(EstadoId);
CREATE INDEX IX_DestinoImagenes_DestinoId ON Catalogo.DestinoImagenes(DestinoId);
CREATE INDEX IX_DestinoCategorias_CategoriaId ON Catalogo.DestinoCategorias(CategoriaDestinoId);
CREATE INDEX IX_Paquetes_DestinoId ON Catalogo.Paquetes(DestinoId);
CREATE INDEX IX_Paquetes_EstadoId ON Catalogo.Paquetes(EstadoId);
CREATE INDEX IX_Tarifas_PaqueteId ON Catalogo.Tarifas(PaqueteId);
CREATE INDEX IX_Tarifas_Vigencia ON Catalogo.Tarifas(VigenciaInicio, VigenciaFin);
GO

CREATE INDEX IX_Clientes_EstadoId ON Cliente.Clientes(EstadoId);
CREATE INDEX IX_DocumentosCliente_ClienteId ON Cliente.DocumentosCliente(ClienteId);
CREATE INDEX IX_DireccionesCliente_ClienteId ON Cliente.DireccionesCliente(ClienteId);
CREATE INDEX IX_Pasajeros_ClienteId ON Cliente.Pasajeros(ClienteId);
CREATE UNIQUE INDEX UX_Pasajeros_Documento ON Cliente.Pasajeros(TipoDocumentoId, NumeroDocumento)
WHERE TipoDocumentoId IS NOT NULL AND NumeroDocumento IS NOT NULL;
GO

CREATE INDEX IX_Proveedores_EstadoId ON Operacion.Proveedores(EstadoId);
CREATE INDEX IX_Proveedores_CiudadId ON Operacion.Proveedores(CiudadId);
CREATE UNIQUE INDEX UX_Proveedores_Documento ON Operacion.Proveedores(TipoDocumentoId, NumeroDocumento)
WHERE TipoDocumentoId IS NOT NULL AND NumeroDocumento IS NOT NULL;
CREATE INDEX IX_ProveedorServicios_ProveedorId ON Operacion.ProveedorServicios(ProveedorId);
CREATE INDEX IX_Guias_EstadoId ON Operacion.Guias(EstadoId);
CREATE INDEX IX_Transportes_EstadoId ON Operacion.Transportes(EstadoId);
CREATE INDEX IX_Hoteles_CiudadId ON Operacion.Hoteles(CiudadId);
CREATE INDEX IX_Habitaciones_HotelId ON Operacion.Habitaciones(HotelId);
CREATE INDEX IX_Salidas_PaqueteId_FechaInicio ON Operacion.Salidas(PaqueteId, FechaInicio);
CREATE INDEX IX_CuposSalida_SalidaId ON Operacion.CuposSalida(SalidaId);
CREATE INDEX IX_AsignacionesOperacion_SalidaId ON Operacion.AsignacionesOperacion(SalidaId);
GO

CREATE INDEX IX_Reservas_ClienteId_FechaReserva ON Ventas.Reservas(ClienteId, FechaReserva DESC);
CREATE INDEX IX_Reservas_SalidaId ON Ventas.Reservas(SalidaId);
CREATE INDEX IX_Reservas_EstadoId ON Ventas.Reservas(EstadoId);
CREATE INDEX IX_ReservaDetalle_ReservaId ON Ventas.ReservaDetalle(ReservaId);
CREATE INDEX IX_ReservaPasajeros_ReservaId ON Ventas.ReservaPasajeros(ReservaId);
CREATE INDEX IX_ReservaHistorialEstados_ReservaId ON Ventas.ReservaHistorialEstados(ReservaId);
CREATE INDEX IX_Compras_ClienteId_FechaCompra ON Ventas.Compras(ClienteId, FechaCompra DESC);
CREATE INDEX IX_Comprobantes_CompraId ON Ventas.Comprobantes(CompraId);
CREATE INDEX IX_TicketsViaje_ReservaId ON Ventas.TicketsViaje(ReservaId);
CREATE INDEX IX_Vouchers_ReservaId ON Ventas.Vouchers(ReservaId);
CREATE INDEX IX_Resenas_DestinoId_Calificacion ON Ventas.Resenas(DestinoId, Calificacion);
CREATE UNIQUE INDEX UX_Resenas_Cliente_Reserva ON Ventas.Resenas(ClienteId, ReservaId) WHERE ReservaId IS NOT NULL;
GO

CREATE INDEX IX_OrdenesPago_ReservaId ON Pagos.OrdenesPago(ReservaId);
CREATE INDEX IX_OrdenesPago_EstadoId ON Pagos.OrdenesPago(EstadoId);
CREATE INDEX IX_Pagos_ReservaId ON Pagos.Pagos(ReservaId);
CREATE INDEX IX_Pagos_OrdenPagoId ON Pagos.Pagos(OrdenPagoId);
CREATE INDEX IX_Pagos_EstadoId ON Pagos.Pagos(EstadoId);
CREATE UNIQUE INDEX UX_Pagos_CodigoTransaccion ON Pagos.Pagos(CodigoTransaccion) WHERE CodigoTransaccion IS NOT NULL;
CREATE INDEX IX_PagoIntentos_OrdenPagoId ON Pagos.PagoIntentos(OrdenPagoId);
CREATE INDEX IX_Reembolsos_PagoId ON Pagos.Reembolsos(PagoId);
GO

CREATE INDEX IX_CampanasPromocionales_EstadoId ON Marketing.CampanasPromocionales(EstadoId);
CREATE INDEX IX_Descuentos_CampanaId ON Marketing.Descuentos(CampanaPromocionalId);
CREATE INDEX IX_Cupones_DescuentoId ON Marketing.Cupones(DescuentoId);
CREATE INDEX IX_LandingPages_CampanaId ON Marketing.LandingPages(CampanaPromocionalId);
CREATE INDEX IX_Leads_Email ON Marketing.Leads(Email);
CREATE INDEX IX_Leads_EstadoId ON Marketing.Leads(EstadoId);
CREATE INDEX IX_LeadActividades_LeadId ON Marketing.LeadActividades(LeadId);
GO

CREATE INDEX IX_MensajesContacto_EstadoId ON Soporte.MensajesContacto(EstadoId);
CREATE INDEX IX_TicketsSoporte_EstadoId_Prioridad ON Soporte.TicketsSoporte(EstadoId, Prioridad);
CREATE INDEX IX_TicketsSoporte_ClienteId ON Soporte.TicketsSoporte(ClienteId);
CREATE INDEX IX_TicketEventos_TicketSoporteId ON Soporte.TicketEventos(TicketSoporteId);
CREATE INDEX IX_Notificaciones_EstadoId_CreadoEn ON Soporte.Notificaciones(EstadoId, CreadoEn DESC);
GO

CREATE INDEX IX_EventosAnalitica_TipoEvento_CreadoEn ON Reportes.EventosAnalitica(TipoEvento, CreadoEn DESC);
CREATE INDEX IX_EventosAnalitica_ClienteId ON Reportes.EventosAnalitica(ClienteId);
CREATE INDEX IX_MetricasDiarias_Fecha ON Reportes.MetricasDiarias(Fecha);
CREATE INDEX IX_DashboardSnapshots_CodigoDashboard_GeneradoEn ON Reportes.DashboardSnapshots(CodigoDashboard, GeneradoEn DESC);
GO

CREATE INDEX IX_AuditoriaCambios_Tabla_CreadoEn ON Auditoria.AuditoriaCambios(EsquemaNombre, TablaNombre, CreadoEn DESC);
CREATE INDEX IX_AuditoriaAccesos_UsuarioId_CreadoEn ON Auditoria.AuditoriaAccesos(UsuarioId, CreadoEn DESC);
CREATE INDEX IX_ErroresAplicacion_Nivel_CreadoEn ON Auditoria.ErroresAplicacion(Nivel, CreadoEn DESC);
GO

INSERT INTO Configuracion.Estados (Grupo, Codigo, Nombre, Descripcion, Orden)
VALUES
(N'GENERAL', N'ACTIVO', N'Activo', N'Registro habilitado', 1),
(N'GENERAL', N'INACTIVO', N'Inactivo', N'Registro deshabilitado', 2),
(N'GENERAL', N'ELIMINADO', N'Eliminado', N'Registro eliminado logicamente', 3),
(N'USUARIO', N'ACTIVO', N'Activo', N'Usuario habilitado', 1),
(N'USUARIO', N'PENDIENTE', N'Pendiente', N'Usuario pendiente de confirmacion', 2),
(N'USUARIO', N'BLOQUEADO', N'Bloqueado', N'Usuario bloqueado', 3),
(N'CLIENTE', N'ACTIVO', N'Activo', N'Cliente habilitado', 1),
(N'CLIENTE', N'INACTIVO', N'Inactivo', N'Cliente deshabilitado', 2),
(N'EMPLEADO', N'ACTIVO', N'Activo', N'Empleado activo', 1),
(N'EMPLEADO', N'INACTIVO', N'Inactivo', N'Empleado inactivo', 2),
(N'DESTINO', N'ACTIVO', N'Activo', N'Destino visible para venta', 1),
(N'DESTINO', N'INACTIVO', N'Inactivo', N'Destino oculto', 2),
(N'PAQUETE', N'ACTIVO', N'Activo', N'Paquete vendible', 1),
(N'PAQUETE', N'INACTIVO', N'Inactivo', N'Paquete no vendible', 2),
(N'SALIDA', N'PROGRAMADA', N'Programada', N'Salida disponible', 1),
(N'SALIDA', N'CERRADA', N'Cerrada', N'Salida sin cupos o cerrada', 2),
(N'SALIDA', N'CANCELADA', N'Cancelada', N'Salida cancelada', 3),
(N'RESERVA', N'BORRADOR', N'Borrador', N'Reserva iniciada', 1),
(N'RESERVA', N'PENDIENTE_PAGO', N'Pendiente de pago', N'Reserva esperando pago', 2),
(N'RESERVA', N'CONFIRMADA', N'Confirmada', N'Reserva pagada y confirmada', 3),
(N'RESERVA', N'CANCELADA', N'Cancelada', N'Reserva cancelada', 4),
(N'RESERVA', N'COMPLETADA', N'Completada', N'Viaje finalizado', 5),
(N'ORDEN_PAGO', N'PENDIENTE', N'Pendiente', N'Orden de pago pendiente', 1),
(N'ORDEN_PAGO', N'APROBADA', N'Aprobada', N'Orden de pago aprobada', 2),
(N'ORDEN_PAGO', N'EXPIRADA', N'Expirada', N'Orden de pago expirada', 3),
(N'PAGO', N'PENDIENTE', N'Pendiente', N'Pago pendiente', 1),
(N'PAGO', N'APROBADO', N'Aprobado', N'Pago aprobado', 2),
(N'PAGO', N'RECHAZADO', N'Rechazado', N'Pago rechazado', 3),
(N'PAGO', N'ANULADO', N'Anulado', N'Pago anulado', 4),
(N'PAGO', N'REEMBOLSADO', N'Reembolsado', N'Pago reembolsado', 5),
(N'REEMBOLSO', N'SOLICITADO', N'Solicitado', N'Reembolso solicitado', 1),
(N'REEMBOLSO', N'APROBADO', N'Aprobado', N'Reembolso aprobado', 2),
(N'REEMBOLSO', N'RECHAZADO', N'Rechazado', N'Reembolso rechazado', 3),
(N'REEMBOLSO', N'PROCESADO', N'Procesado', N'Reembolso procesado', 4),
(N'COMPRA', N'EMITIDA', N'Emitida', N'Compra emitida', 1),
(N'COMPRA', N'ANULADA', N'Anulada', N'Compra anulada', 2),
(N'COMPROBANTE', N'EMITIDO', N'Emitido', N'Comprobante emitido', 1),
(N'COMPROBANTE', N'ANULADO', N'Anulado', N'Comprobante anulado', 2),
(N'TICKET_VIAJE', N'EMITIDO', N'Emitido', N'Ticket de viaje emitido', 1),
(N'TICKET_VIAJE', N'ANULADO', N'Anulado', N'Ticket de viaje anulado', 2),
(N'VENTA_DOCUMENTO', N'EMITIDO', N'Emitido', N'Documento de venta emitido', 1),
(N'VENTA_DOCUMENTO', N'ANULADO', N'Anulado', N'Documento de venta anulado', 2),
(N'PROVEEDOR', N'ACTIVO', N'Activo', N'Proveedor activo', 1),
(N'PROVEEDOR', N'INACTIVO', N'Inactivo', N'Proveedor inactivo', 2),
(N'SERVICIO', N'ACTIVO', N'Activo', N'Servicio activo', 1),
(N'SERVICIO', N'INACTIVO', N'Inactivo', N'Servicio inactivo', 2),
(N'GUIA', N'ACTIVO', N'Activo', N'Guia activo', 1),
(N'GUIA', N'INACTIVO', N'Inactivo', N'Guia inactivo', 2),
(N'TRANSPORTE', N'ACTIVO', N'Activo', N'Transporte activo', 1),
(N'TRANSPORTE', N'INACTIVO', N'Inactivo', N'Transporte inactivo', 2),
(N'HOTEL', N'ACTIVO', N'Activo', N'Hotel activo', 1),
(N'HOTEL', N'INACTIVO', N'Inactivo', N'Hotel inactivo', 2),
(N'HABITACION', N'ACTIVO', N'Activo', N'Habitacion activa', 1),
(N'HABITACION', N'INACTIVO', N'Inactivo', N'Habitacion inactiva', 2),
(N'SOPORTE', N'ABIERTO', N'Abierto', N'Ticket abierto', 1),
(N'SOPORTE', N'EN_PROCESO', N'En proceso', N'Ticket en atencion', 2),
(N'SOPORTE', N'CERRADO', N'Cerrado', N'Ticket cerrado', 3),
(N'RESENA', N'PENDIENTE', N'Pendiente', N'Resena pendiente de moderacion', 1),
(N'RESENA', N'APROBADA', N'Aprobada', N'Resena publicada', 2),
(N'RESENA', N'RECHAZADA', N'Rechazada', N'Resena rechazada', 3),
(N'LEAD', N'NUEVO', N'Nuevo', N'Lead capturado', 1),
(N'LEAD', N'CONTACTADO', N'Contactado', N'Lead contactado', 2),
(N'LEAD', N'CONVERTIDO', N'Convertido', N'Lead convertido en cliente', 3),
(N'LEAD', N'DESCARTADO', N'Descartado', N'Lead descartado', 4),
(N'CAMPANA', N'ACTIVA', N'Activa', N'Campana activa', 1),
(N'CAMPANA', N'PAUSADA', N'Pausada', N'Campana pausada', 2),
(N'CAMPANA', N'FINALIZADA', N'Finalizada', N'Campana finalizada', 3),
(N'CONTENIDO', N'BORRADOR', N'Borrador', N'Contenido en borrador', 1),
(N'CONTENIDO', N'PUBLICADO', N'Publicado', N'Contenido publicado', 2),
(N'CONTENIDO', N'ARCHIVADO', N'Archivado', N'Contenido archivado', 3),
(N'LANDING', N'ACTIVA', N'Activa', N'Landing activa', 1),
(N'LANDING', N'INACTIVA', N'Inactiva', N'Landing inactiva', 2),
(N'NOTIFICACION', N'PENDIENTE', N'Pendiente', N'Notificacion pendiente', 1),
(N'NOTIFICACION', N'ENVIADA', N'Enviada', N'Notificacion enviada', 2),
(N'NOTIFICACION', N'ERROR', N'Error', N'Notificacion con error', 3);
GO

INSERT INTO Seguridad.Roles (Codigo, Nombre, Descripcion, EsSistema)
VALUES
(N'ADMINISTRADOR', N'Administrador', N'Acceso completo a la plataforma', 1),
(N'CLIENTE', N'Cliente', N'Usuario comprador de paquetes turisticos', 1),
(N'EMPLEADO', N'Empleado', N'Usuario operativo o administrativo', 1);
GO

INSERT INTO Seguridad.Permisos (Codigo, Modulo, Accion, Descripcion)
VALUES
(N'SEGURIDAD_USUARIOS_LEER', N'Seguridad', N'Leer usuarios', N'Permite consultar usuarios'),
(N'SEGURIDAD_USUARIOS_GESTIONAR', N'Seguridad', N'Gestionar usuarios', N'Permite crear y modificar usuarios'),
(N'CATALOGO_DESTINOS_LEER', N'Catalogo', N'Leer destinos', N'Permite consultar destinos'),
(N'CATALOGO_DESTINOS_GESTIONAR', N'Catalogo', N'Gestionar destinos', N'Permite administrar destinos y paquetes'),
(N'VENTAS_RESERVAS_LEER', N'Ventas', N'Leer reservas', N'Permite consultar reservas'),
(N'VENTAS_RESERVAS_GESTIONAR', N'Ventas', N'Gestionar reservas', N'Permite crear y modificar reservas'),
(N'PAGOS_LEER', N'Pagos', N'Leer pagos', N'Permite consultar pagos'),
(N'PAGOS_GESTIONAR', N'Pagos', N'Gestionar pagos', N'Permite registrar pagos y reembolsos'),
(N'MARKETING_GESTIONAR', N'Marketing', N'Gestionar marketing', N'Permite administrar campanas y leads'),
(N'SOPORTE_GESTIONAR', N'Soporte', N'Gestionar soporte', N'Permite atender tickets de soporte'),
(N'REPORTES_LEER', N'Reportes', N'Leer reportes', N'Permite visualizar dashboards y metricas'),
(N'AUDITORIA_LEER', N'Auditoria', N'Leer auditoria', N'Permite revisar auditoria del sistema');
GO

INSERT INTO Seguridad.RolPermisos (RolId, PermisoId)
SELECT r.RolId, p.PermisoId
FROM Seguridad.Roles r
CROSS JOIN Seguridad.Permisos p
WHERE r.Codigo = N'ADMINISTRADOR';
GO

INSERT INTO Seguridad.RolPermisos (RolId, PermisoId)
SELECT r.RolId, p.PermisoId
FROM Seguridad.Roles r
JOIN Seguridad.Permisos p ON p.Codigo IN (
    N'CATALOGO_DESTINOS_LEER',
    N'VENTAS_RESERVAS_LEER',
    N'VENTAS_RESERVAS_GESTIONAR',
    N'PAGOS_LEER'
)
WHERE r.Codigo = N'CLIENTE';
GO

INSERT INTO Seguridad.RolPermisos (RolId, PermisoId)
SELECT r.RolId, p.PermisoId
FROM Seguridad.Roles r
JOIN Seguridad.Permisos p ON p.Codigo IN (
    N'CATALOGO_DESTINOS_LEER',
    N'CATALOGO_DESTINOS_GESTIONAR',
    N'VENTAS_RESERVAS_LEER',
    N'VENTAS_RESERVAS_GESTIONAR',
    N'PAGOS_LEER',
    N'SOPORTE_GESTIONAR',
    N'REPORTES_LEER'
)
WHERE r.Codigo = N'EMPLEADO';
GO

INSERT INTO Configuracion.TiposDocumento (Codigo, Nombre, LongitudMinima, LongitudMaxima)
VALUES
(N'DNI', N'Documento Nacional de Identidad', 8, 8),
(N'CE', N'Carne de Extranjeria', 8, 12),
(N'PASAPORTE', N'Pasaporte', 6, 20),
(N'RUC', N'Registro Unico de Contribuyente', 11, 11);
GO

INSERT INTO Configuracion.Monedas (CodigoIso, Nombre, Simbolo, EsMonedaBase)
VALUES
(N'PEN', N'Sol peruano', N'S/', 1),
(N'USD', N'Dolar estadounidense', N'$', 0);
GO

INSERT INTO Configuracion.ParametrosSistema (Clave, Valor, TipoDato, Descripcion)
VALUES
(N'APP_NOMBRE', N'WayPath', N'STRING', N'Nombre publico de la plataforma'),
(N'MONEDA_DEFAULT', N'PEN', N'STRING', N'Moneda por defecto'),
(N'IGV_PORCENTAJE', N'18.00', N'DECIMAL', N'Impuesto general a las ventas'),
(N'RESERVA_EXPIRACION_MINUTOS', N'30', N'INT', N'Tiempo maximo para completar el pago'),
(N'SOPORTE_EMAIL', N'hola@waypath.com', N'STRING', N'Correo principal de soporte'),
(N'SOPORTE_TELEFONO', N'+51 987 654 321', N'STRING', N'Telefono principal de soporte'),
(N'SEGURIDAD_JWT_EXPIRACION_MINUTOS', N'60', N'INT', N'Duracion sugerida del token JWT'),
(N'VENTAS_CODIGO_PREFIJO_RESERVA', N'WP', N'STRING', N'Prefijo para codigos de reserva');
GO

INSERT INTO Configuracion.TerminosLegales (Tipo, Version, Titulo, Contenido, EsVigente, PublicadoEn)
VALUES
(N'PRIVACIDAD', N'1.0', N'Politica de privacidad WayPath', N'Politica inicial de privacidad para usuarios y clientes de WayPath.', 1, SYSUTCDATETIME()),
(N'TERMINOS', N'1.0', N'Terminos y condiciones WayPath', N'Terminos iniciales de uso y contratacion de servicios turisticos WayPath.', 1, SYSUTCDATETIME());
GO

INSERT INTO Configuracion.PoliticasCancelacion (Nombre, Descripcion, HorasAntesInicio, PorcentajePenalidad)
VALUES
(N'Politica flexible', N'Cancelacion sin penalidad hasta 72 horas antes del inicio del viaje.', 72, 0.00),
(N'Politica estandar', N'Cancelacion con penalidad parcial dentro de las 72 horas previas al viaje.', 72, 25.00),
(N'Politica estricta', N'Cancelacion con penalidad alta dentro de los 7 dias previos al viaje.', 168, 50.00);
GO

INSERT INTO Operacion.Areas (Codigo, Nombre, Descripcion)
VALUES
(N'ADMINISTRACION', N'Administracion', N'Gestion administrativa general'),
(N'VENTAS', N'Ventas', N'Gestion comercial y reservas'),
(N'OPERACIONES', N'Operaciones', N'Ejecucion de viajes y proveedores'),
(N'MARKETING', N'Marketing', N'Campanas, leads y promociones'),
(N'SOPORTE', N'Soporte', N'Atencion al cliente');
GO

INSERT INTO Catalogo.Paises (CodigoIso2, Nombre)
VALUES (N'PE', N'Peru');
GO

INSERT INTO Catalogo.Regiones (PaisId, Nombre)
SELECT p.PaisId, v.Nombre
FROM Catalogo.Paises p
CROSS JOIN (VALUES
(N'Cusco'),
(N'Ica'),
(N'Arequipa'),
(N'Puno'),
(N'Piura'),
(N'Loreto'),
(N'Ancash'),
(N'Lima')
) v(Nombre)
WHERE p.CodigoIso2 = N'PE';
GO

INSERT INTO Catalogo.Ciudades (RegionId, Nombre)
SELECT r.RegionId, v.Ciudad
FROM (VALUES
(N'Cusco', N'Cusco'),
(N'Ica', N'Ica'),
(N'Ica', N'Paracas'),
(N'Ica', N'Nazca'),
(N'Arequipa', N'Arequipa'),
(N'Puno', N'Puno'),
(N'Piura', N'Mancora'),
(N'Loreto', N'Iquitos'),
(N'Ancash', N'Huaraz'),
(N'Lima', N'Barranca'),
(N'Lima', N'Lima'),
(N'Lima', N'Miraflores')
) v(Region, Ciudad)
JOIN Catalogo.Regiones r ON r.Nombre = v.Region;
GO

INSERT INTO Catalogo.MediaAssets (Tipo, Url, NombreArchivo, MimeType, TextoAlternativo)
VALUES
(N'IMAGEN', N'Img/machupichu.jpg', N'machupichu.jpg', N'image/jpeg', N'Machu Picchu'),
(N'IMAGEN', N'Img/MONTAÑADELOS7COLORES.webp', N'MONTAÑADELOS7COLORES.webp', N'image/webp', N'Montana de los 7 colores'),
(N'IMAGEN', N'Img/OASISDEHUACACHINA.jpg', N'OASISDEHUACACHINA.jpg', N'image/jpeg', N'Oasis de Huacachina'),
(N'IMAGEN', N'Img/ISLASBALLESTAS.webp', N'ISLASBALLESTAS.webp', N'image/webp', N'Islas Ballestas'),
(N'IMAGEN', N'Img/CAÑONDELCOLCA.webp', N'CAÑONDELCOLCA.webp', N'image/webp', N'Canon del Colca'),
(N'IMAGEN', N'Img/LAGOTITICACA.webp', N'LAGOTITICACA.webp', N'image/webp', N'Lago Titicaca'),
(N'IMAGEN', N'Img/nasca.jpg', N'nasca.jpg', N'image/jpeg', N'Lineas de Nazca'),
(N'IMAGEN', N'Img/MANCORA.jpg', N'MANCORA.jpg', N'image/jpeg', N'Mancora'),
(N'IMAGEN', N'Img/RESERVANACIONALPACAYA.jpg', N'RESERVANACIONALPACAYA.jpg', N'image/jpeg', N'Reserva Nacional Pacaya Samiria'),
(N'IMAGEN', N'Img/PARQUENACIONALHUASCARAN.webp', N'PARQUENACIONALHUASCARAN.webp', N'image/webp', N'Parque Nacional Huascaran'),
(N'IMAGEN', N'Img/caral.jpg', N'caral.jpg', N'image/jpeg', N'Ciudad Sagrada de Caral'),
(N'IMAGEN', N'Img/yape.jpg', N'yape.jpg', N'image/jpeg', N'Yape');
GO

INSERT INTO Catalogo.UbicacionesTuristicas (CiudadId, Nombre, Direccion, Latitud, Longitud)
SELECT c.CiudadId, v.Nombre, v.Direccion, v.Latitud, v.Longitud
FROM (VALUES
(N'Cusco', N'Machu Picchu', N'Provincia de Urubamba, Cusco', -13.163100, -72.545000),
(N'Cusco', N'Montana de los 7 Colores', N'Vinicunca, Cusco', -13.869700, -71.302200),
(N'Ica', N'Oasis de Huacachina', N'Huacachina, Ica', -14.087500, -75.763300),
(N'Paracas', N'Islas Ballestas', N'Paracas, Ica', -13.733300, -76.400000),
(N'Arequipa', N'Canon del Colca', N'Caylloma, Arequipa', -15.609400, -71.905600),
(N'Puno', N'Lago Titicaca', N'Puno, Peru', -15.840200, -69.335400),
(N'Nazca', N'Lineas de Nazca', N'Nazca, Ica', -14.739000, -75.130000),
(N'Mancora', N'Mancora', N'Talara, Piura', -4.107800, -81.047500),
(N'Iquitos', N'Reserva Nacional Pacaya Samiria', N'Loreto, Peru', -5.250000, -74.500000),
(N'Huaraz', N'Parque Nacional Huascaran', N'Ancash, Peru', -9.333300, -77.400000),
(N'Barranca', N'Ciudad Sagrada de Caral', N'Barranca, Lima', -10.891700, -77.520600)
) v(Ciudad, Nombre, Direccion, Latitud, Longitud)
JOIN Catalogo.Ciudades c ON c.Nombre = v.Ciudad;
GO

INSERT INTO Catalogo.Destinos (
    CiudadId,
    UbicacionTuristicaId,
    EstadoId,
    Nombre,
    Slug,
    DescripcionCorta,
    DescripcionLarga,
    AltitudMetros,
    EsDestacado
)
SELECT c.CiudadId, u.UbicacionTuristicaId, e.EstadoId, v.Nombre, v.Slug, v.DescripcionCorta, v.DescripcionLarga, v.AltitudMetros, v.EsDestacado
FROM (VALUES
(N'Cusco', N'Machu Picchu', N'machu-picchu', N'Explora la legendaria ciudadela inca, maravilla del mundo.', N'Experiencia cultural y arqueologica en uno de los destinos mas reconocidos del Peru.', 2430, 1),
(N'Cusco', N'Montana de los 7 Colores', N'montana-7-colores', N'Desafia la altura y conectate con la naturaleza andina.', N'Ruta de aventura hacia Vinicunca, paisaje natural de alta montana.', 5200, 1),
(N'Ica', N'Oasis de Huacachina', N'oasis-huacachina', N'Oasis natural rodeado de gigantescas dunas de arena.', N'Destino de aventura y relax en el desierto de Ica.', 406, 1),
(N'Paracas', N'Islas Ballestas', N'islas-ballestas', N'Santuario de vida marina frente al Pacifico.', N'Recorrido nautico para observar fauna marina y paisajes costeros.', 0, 0),
(N'Arequipa', N'Canon del Colca', N'canon-colca', N'Uno de los canones mas profundos del planeta.', N'Experiencia natural y cultural en el valle del Colca.', 3270, 0),
(N'Puno', N'Lago Titicaca', N'lago-titicaca', N'Lago navegable alto con comunidades y cultura viva.', N'Experiencia cultural en islas y comunidades del altiplano peruano.', 3812, 1),
(N'Nazca', N'Lineas de Nazca', N'lineas-nazca', N'Geoglifos ancestrales en el desierto de Ica.', N'Sobrevuelo y experiencia cultural para conocer los misterios de Nazca.', 520, 0),
(N'Mancora', N'Mancora', N'mancora', N'Playas del norte peruano para sol, descanso y aventura.', N'Destino de playa con gastronomia, hoteles y actividades marinas.', 5, 1),
(N'Iquitos', N'Reserva Nacional Pacaya Samiria', N'reserva-pacaya-samiria', N'Amazonia viva entre rios, fauna y selva tropical.', N'Experiencia de naturaleza y aventura en la selva peruana.', 104, 0),
(N'Huaraz', N'Parque Nacional Huascaran', N'parque-huascaran', N'Cordillera Blanca, nevados, lagunas y trekking.', N'Destino de aventura de montana para caminatas y paisajes naturales.', 3052, 0),
(N'Barranca', N'Ciudad Sagrada de Caral', N'ciudad-sagrada-caral', N'Civilizacion antigua con arquitectura monumental.', N'Ruta arqueologica hacia una de las civilizaciones mas antiguas de America.', 350, 0)
) v(Ciudad, Nombre, Slug, DescripcionCorta, DescripcionLarga, AltitudMetros, EsDestacado)
JOIN Catalogo.Ciudades c ON c.Nombre = v.Ciudad
JOIN Catalogo.UbicacionesTuristicas u ON u.Nombre = v.Nombre
CROSS JOIN Configuracion.Estados e
WHERE e.Grupo = N'DESTINO' AND e.Codigo = N'ACTIVO';
GO

INSERT INTO Catalogo.CategoriasDestino (Codigo, Nombre, Descripcion)
VALUES
(N'CULTURAL', N'Cultural', N'Experiencias de cultura viva e historia'),
(N'ARQUEOLOGIA', N'Arqueologia', N'Sitios arqueologicos y patrimonio'),
(N'AVENTURA', N'Aventura', N'Actividades de aventura y naturaleza'),
(N'NATURALEZA', N'Naturaleza', N'Paisajes naturales y biodiversidad'),
(N'PLAYA', N'Playa', N'Destinos de sol y playa'),
(N'RELAX', N'Relax', N'Descanso y experiencias tranquilas');
GO

INSERT INTO Catalogo.DestinoCategorias (DestinoId, CategoriaDestinoId)
SELECT d.DestinoId, c.CategoriaDestinoId
FROM (VALUES
(N'machu-picchu', N'CULTURAL'),
(N'machu-picchu', N'ARQUEOLOGIA'),
(N'montana-7-colores', N'AVENTURA'),
(N'montana-7-colores', N'NATURALEZA'),
(N'oasis-huacachina', N'AVENTURA'),
(N'oasis-huacachina', N'NATURALEZA'),
(N'islas-ballestas', N'NATURALEZA'),
(N'canon-colca', N'NATURALEZA'),
(N'canon-colca', N'AVENTURA'),
(N'lago-titicaca', N'CULTURAL'),
(N'lago-titicaca', N'NATURALEZA'),
(N'lineas-nazca', N'ARQUEOLOGIA'),
(N'mancora', N'PLAYA'),
(N'mancora', N'RELAX'),
(N'reserva-pacaya-samiria', N'NATURALEZA'),
(N'reserva-pacaya-samiria', N'AVENTURA'),
(N'parque-huascaran', N'AVENTURA'),
(N'parque-huascaran', N'NATURALEZA'),
(N'ciudad-sagrada-caral', N'ARQUEOLOGIA'),
(N'ciudad-sagrada-caral', N'CULTURAL')
) v(SlugDestino, CodigoCategoria)
JOIN Catalogo.Destinos d ON d.Slug = v.SlugDestino
JOIN Catalogo.CategoriasDestino c ON c.Codigo = v.CodigoCategoria;
GO

INSERT INTO Catalogo.DestinoImagenes (DestinoId, MediaAssetId, EsPrincipal, Orden)
SELECT d.DestinoId, m.MediaAssetId, 1, 1
FROM (VALUES
(N'machu-picchu', N'Img/machupichu.jpg'),
(N'montana-7-colores', N'Img/MONTAÑADELOS7COLORES.webp'),
(N'oasis-huacachina', N'Img/OASISDEHUACACHINA.jpg'),
(N'islas-ballestas', N'Img/ISLASBALLESTAS.webp'),
(N'canon-colca', N'Img/CAÑONDELCOLCA.webp'),
(N'lago-titicaca', N'Img/LAGOTITICACA.webp'),
(N'lineas-nazca', N'Img/nasca.jpg'),
(N'mancora', N'Img/MANCORA.jpg'),
(N'reserva-pacaya-samiria', N'Img/RESERVANACIONALPACAYA.jpg'),
(N'parque-huascaran', N'Img/PARQUENACIONALHUASCARAN.webp'),
(N'ciudad-sagrada-caral', N'Img/caral.jpg')
) v(SlugDestino, Url)
JOIN Catalogo.Destinos d ON d.Slug = v.SlugDestino
JOIN Catalogo.MediaAssets m ON m.Url = v.Url;
GO

INSERT INTO Catalogo.AtraccionesDestino (DestinoId, Nombre, Descripcion, DuracionEstimadaMinutos, Orden)
SELECT d.DestinoId, v.NombreAtraccion, v.Descripcion, v.Duracion, v.Orden
FROM (VALUES
(N'machu-picchu', N'Ciudadela inca', N'Recorrido principal por terrazas, templos y miradores.', 180, 1),
(N'montana-7-colores', N'Caminata Vinicunca', N'Ruta de trekking de alta montana.', 300, 1),
(N'oasis-huacachina', N'Sandboarding', N'Actividad de aventura sobre dunas.', 90, 1),
(N'islas-ballestas', N'Paseo nautico', N'Navegacion para observar fauna marina.', 120, 1),
(N'lago-titicaca', N'Islas flotantes', N'Visita cultural a comunidades locales.', 180, 1),
(N'mancora', N'Playa Mancora', N'Dia de playa y actividades marinas.', 240, 1)
) v(SlugDestino, NombreAtraccion, Descripcion, Duracion, Orden)
JOIN Catalogo.Destinos d ON d.Slug = v.SlugDestino;
GO

INSERT INTO Catalogo.Paquetes (
    DestinoId,
    EstadoId,
    Nombre,
    Slug,
    Descripcion,
    DuracionDias,
    DuracionNoches,
    NivelDificultad,
    EsDestacado
)
SELECT d.DestinoId, e.EstadoId, v.Nombre, v.Slug, v.Descripcion, v.Dias, v.Noches, v.Nivel, v.EsDestacado
FROM (VALUES
(N'machu-picchu', N'Aventura en Machu Picchu', N'paquete-machu-picchu', N'Paquete turistico hacia Machu Picchu con guia y entradas.', 5, 4, N'MEDIO', 1),
(N'montana-7-colores', N'Ruta Montana de los 7 Colores', N'paquete-montana-7-colores', N'Caminata guiada hacia Vinicunca.', 1, 0, N'ALTO', 1),
(N'oasis-huacachina', N'Escapada Oasis de Huacachina', N'paquete-huacachina', N'Experiencia de aventura en dunas y oasis.', 1, 0, N'BAJO', 1),
(N'islas-ballestas', N'Experiencia Islas Ballestas', N'paquete-islas-ballestas', N'Navegacion en Paracas hacia Islas Ballestas.', 1, 0, N'BAJO', 0),
(N'canon-colca', N'Canon del Colca Natural', N'paquete-canon-colca', N'Ruta por miradores y valle del Colca.', 2, 1, N'MEDIO', 0),
(N'lago-titicaca', N'Cultura Viva en Lago Titicaca', N'paquete-lago-titicaca', N'Visita cultural a islas del Titicaca.', 2, 1, N'BAJO', 1),
(N'lineas-nazca', N'Sobrevuelo Lineas de Nazca', N'paquete-lineas-nazca', N'Sobrevuelo turistico y experiencia cultural.', 1, 0, N'BAJO', 0),
(N'mancora', N'Escapada a Mancora', N'paquete-mancora', N'Paquete de playa de 4 dias y 3 noches.', 4, 3, N'BAJO', 1),
(N'reserva-pacaya-samiria', N'Aventura Amazonica Pacaya Samiria', N'paquete-pacaya-samiria', N'Experiencia amazonica con navegacion y naturaleza.', 4, 3, N'MEDIO', 0),
(N'parque-huascaran', N'Trekking Parque Huascaran', N'paquete-huascaran', N'Trekking y naturaleza en la Cordillera Blanca.', 2, 1, N'ALTO', 0),
(N'ciudad-sagrada-caral', N'Ruta Ciudad Sagrada de Caral', N'paquete-caral', N'Ruta arqueologica de dia completo.', 1, 0, N'BAJO', 0)
) v(SlugDestino, Nombre, Slug, Descripcion, Dias, Noches, Nivel, EsDestacado)
JOIN Catalogo.Destinos d ON d.Slug = v.SlugDestino
CROSS JOIN Configuracion.Estados e
WHERE e.Grupo = N'PAQUETE' AND e.Codigo = N'ACTIVO';
GO

INSERT INTO Catalogo.PaqueteItinerario (PaqueteId, DiaNumero, Titulo, Descripcion, Orden)
SELECT p.PaqueteId, 1, N'Inicio de experiencia', N'Bienvenida, coordinacion operativa y desarrollo de actividades principales del paquete.', 1
FROM Catalogo.Paquetes p;
GO

INSERT INTO Catalogo.PaqueteIncluye (PaqueteId, Descripcion, Orden)
SELECT p.PaqueteId, v.Descripcion, v.Orden
FROM Catalogo.Paquetes p
CROSS JOIN (VALUES
(N'Asistencia de coordinacion WayPath', 1),
(N'Guia local segun disponibilidad del paquete', 2),
(N'Soporte al viajero durante la experiencia', 3)
) v(Descripcion, Orden);
GO

INSERT INTO Catalogo.PaqueteNoIncluye (PaqueteId, Descripcion, Orden)
SELECT p.PaqueteId, v.Descripcion, v.Orden
FROM Catalogo.Paquetes p
CROSS JOIN (VALUES
(N'Gastos personales', 1),
(N'Alimentos no especificados en el itinerario', 2),
(N'Servicios adicionales no contratados', 3)
) v(Descripcion, Orden);
GO

INSERT INTO Catalogo.PaquetePoliticas (PaqueteId, PoliticaCancelacionId, EsPrincipal)
SELECT p.PaqueteId, pc.PoliticaCancelacionId, 1
FROM Catalogo.Paquetes p
JOIN Configuracion.PoliticasCancelacion pc ON pc.Nombre = N'Politica estandar';
GO

INSERT INTO Catalogo.Impuestos (Codigo, Nombre, Porcentaje)
VALUES (N'IGV', N'Impuesto General a las Ventas', 18.0000);
GO

INSERT INTO Catalogo.Tarifas (PaqueteId, MonedaId, EstadoId, TipoTarifa, PrecioBase, PrecioNino, VigenciaInicio, VigenciaFin)
SELECT p.PaqueteId, m.MonedaId, e.EstadoId, N'PUBLICA', v.PrecioBase, NULL, CONVERT(DATE, '2026-01-01'), NULL
FROM (VALUES
(N'paquete-machu-picchu', 950.00),
(N'paquete-montana-7-colores', 180.00),
(N'paquete-huacachina', 160.00),
(N'paquete-islas-ballestas', 220.00),
(N'paquete-canon-colca', 130.00),
(N'paquete-lago-titicaca', 110.00),
(N'paquete-lineas-nazca', 450.00),
(N'paquete-mancora', 350.00),
(N'paquete-pacaya-samiria', 1200.00),
(N'paquete-huascaran', 90.00),
(N'paquete-caral', 250.00)
) v(SlugPaquete, PrecioBase)
JOIN Catalogo.Paquetes p ON p.Slug = v.SlugPaquete
JOIN Configuracion.Monedas m ON m.CodigoIso = N'PEN'
JOIN Configuracion.Estados e ON e.Grupo = N'PAQUETE' AND e.Codigo = N'ACTIVO';
GO

INSERT INTO Catalogo.TarifaImpuestos (TarifaId, ImpuestoId)
SELECT t.TarifaId, i.ImpuestoId
FROM Catalogo.Tarifas t
CROSS JOIN Catalogo.Impuestos i
WHERE i.Codigo = N'IGV';
GO

INSERT INTO Operacion.Salidas (PaqueteId, EstadoId, FechaInicio, FechaFin, CupoTotal, CupoReservado, PuntoEncuentro)
SELECT p.PaqueteId, e.EstadoId, v.FechaInicio, v.FechaFin, v.CupoTotal, 0, v.PuntoEncuentro
FROM (VALUES
(N'paquete-machu-picchu', CONVERT(DATE, '2026-10-15'), CONVERT(DATE, '2026-10-19'), 24, N'Plaza de Armas de Cusco'),
(N'paquete-montana-7-colores', CONVERT(DATE, '2026-10-20'), CONVERT(DATE, '2026-10-20'), 18, N'Centro historico de Cusco'),
(N'paquete-huacachina', CONVERT(DATE, '2026-10-12'), CONVERT(DATE, '2026-10-12'), 30, N'Terminal turistico de Ica'),
(N'paquete-islas-ballestas', CONVERT(DATE, '2026-10-18'), CONVERT(DATE, '2026-10-18'), 25, N'Muelle El Chaco, Paracas'),
(N'paquete-canon-colca', CONVERT(DATE, '2026-11-05'), CONVERT(DATE, '2026-11-06'), 20, N'Centro de Arequipa'),
(N'paquete-lago-titicaca', CONVERT(DATE, '2026-11-10'), CONVERT(DATE, '2026-11-11'), 22, N'Puerto de Puno'),
(N'paquete-lineas-nazca', CONVERT(DATE, '2026-10-25'), CONVERT(DATE, '2026-10-25'), 12, N'Aerodromo de Nazca'),
(N'paquete-mancora', CONVERT(DATE, '2026-12-01'), CONVERT(DATE, '2026-12-04'), 28, N'Plaza principal de Mancora'),
(N'paquete-pacaya-samiria', CONVERT(DATE, '2026-11-20'), CONVERT(DATE, '2026-11-23'), 14, N'Puerto de Iquitos'),
(N'paquete-huascaran', CONVERT(DATE, '2026-10-30'), CONVERT(DATE, '2026-10-31'), 16, N'Plaza de Huaraz'),
(N'paquete-caral', CONVERT(DATE, '2026-10-22'), CONVERT(DATE, '2026-10-22'), 20, N'Punto de salida Lima')
) v(SlugPaquete, FechaInicio, FechaFin, CupoTotal, PuntoEncuentro)
JOIN Catalogo.Paquetes p ON p.Slug = v.SlugPaquete
JOIN Configuracion.Estados e ON e.Grupo = N'SALIDA' AND e.Codigo = N'PROGRAMADA';
GO

INSERT INTO Operacion.CuposSalida (SalidaId, TipoCupo, CupoTotal, CupoReservado)
SELECT s.SalidaId, N'GENERAL', s.CupoTotal, 0
FROM Operacion.Salidas s;
GO

INSERT INTO Catalogo.PaginasContenido (Slug, Titulo, ContenidoHtml, EstadoId, PublicadoEn)
SELECT v.Slug, v.Titulo, v.ContenidoHtml, e.EstadoId, SYSUTCDATETIME()
FROM (VALUES
(N'inicio', N'WayPath - Descubre el Peru', N'Pagina principal de WayPath.'),
(N'servicios', N'Nuestros servicios', N'Servicios turisticos de WayPath.'),
(N'destinos', N'Destinos', N'Catalogo de destinos turisticos.'),
(N'compras', N'Mis compras', N'Historial de compras y reservas.'),
(N'resenas', N'Nuestras resenas', N'Opiniones de viajeros.'),
(N'contacto', N'Contacto', N'Canales de atencion y formulario de contacto.')
) v(Slug, Titulo, ContenidoHtml)
JOIN Configuracion.Estados e ON e.Grupo = N'CONTENIDO' AND e.Codigo = N'PUBLICADO';
GO

INSERT INTO Catalogo.FAQs (PaginaContenidoId, Pregunta, Respuesta, Orden)
SELECT pc.PaginaContenidoId, v.Pregunta, v.Respuesta, v.Orden
FROM Catalogo.PaginasContenido pc
CROSS JOIN (VALUES
(N'¿Como puedo modificar o cancelar mi reserva?', N'Puedes gestionar tus reservas desde la seccion Mis compras o contactar con soporte.', 1),
(N'¿Cuales son los metodos de pago aceptados?', N'Aceptamos tarjetas, Yape, Plin y transferencia segun disponibilidad.', 2),
(N'¿Cuando recibire la confirmacion de mi viaje?', N'La confirmacion se genera despues de aprobarse el pago de la reserva.', 3),
(N'¿Ofrecen asistencia durante el viaje?', N'Si, WayPath ofrece asistencia al viajero durante la experiencia contratada.', 4)
) v(Pregunta, Respuesta, Orden)
WHERE pc.Slug = N'contacto';
GO

INSERT INTO Catalogo.MenuNavegacion (MenuPadreId, Texto, Url, Icono, Orden, EsActivo)
VALUES
(NULL, N'Nuestros servicios', N'servicios.html', N'fa-concierge-bell', 1, 1),
(NULL, N'Destinos', N'Destinos.html', N'fa-map-location-dot', 2, 1),
(NULL, N'Mis compras', N'compras.html', N'fa-bag-shopping', 3, 1),
(NULL, N'Nuestras resenas', N'reseñas.html', N'fa-star', 4, 1),
(NULL, N'Contacto', N'contacto.html', N'fa-envelope', 5, 1),
(NULL, N'Dashboards', N'#', N'fa-chart-line', 6, 1),
(NULL, N'Landing Pages', N'#', N'fa-rocket', 7, 1);
GO

INSERT INTO Catalogo.MenuNavegacion (MenuPadreId, Texto, Url, Icono, Orden, EsActivo)
SELECT m.MenuNavegacionId, v.Texto, v.Url, v.Icono, v.Orden, 1
FROM Catalogo.MenuNavegacion m
JOIN (VALUES
(N'Dashboards', N'Rendimiento Comercial', N'dashboards/dashboard-ventas.html', N'fa-chart-bar', 1),
(N'Dashboards', N'Marketing y Trafico', N'dashboards/dashboard-marketing.html', N'fa-bullhorn', 2),
(N'Landing Pages', N'Escapada a Mancora', N'landings_pages/landing-mancora.html', N'fa-umbrella-beach', 1),
(N'Landing Pages', N'Guia del Viajero', N'landings_pages/landing-guia.html', N'fa-book-open', 2)
) v(Padre, Texto, Url, Icono, Orden) ON m.Texto = v.Padre;
GO

INSERT INTO Pagos.MetodosPago (Codigo, Nombre, RequierePasarela)
VALUES
(N'TARJETA_CREDITO', N'Tarjeta de credito', 1),
(N'TARJETA_DEBITO', N'Tarjeta de debito', 1),
(N'YAPE', N'Yape', 1),
(N'PLIN', N'Plin', 1),
(N'TRANSFERENCIA', N'Transferencia bancaria', 0),
(N'EFECTIVO', N'Efectivo', 0);
GO

INSERT INTO Soporte.CategoriasSoporte (Codigo, Nombre)
VALUES
(N'RESERVA', N'Reservas'),
(N'PAGO', N'Pagos'),
(N'VIAJE', N'Asistencia en viaje'),
(N'REEMBOLSO', N'Reembolsos'),
(N'GENERAL', N'Consulta general');
GO

INSERT INTO Soporte.CanalesNotificacion (Codigo, Nombre)
VALUES
(N'EMAIL', N'Correo electronico'),
(N'SMS', N'Mensaje SMS'),
(N'WHATSAPP', N'WhatsApp'),
(N'PUSH', N'Notificacion push');
GO

INSERT INTO Soporte.PlantillasNotificacion (CanalNotificacionId, Codigo, Asunto, Cuerpo)
SELECT c.CanalNotificacionId, v.Codigo, v.Asunto, v.Cuerpo
FROM Soporte.CanalesNotificacion c
JOIN (VALUES
(N'EMAIL', N'RESERVA_CONFIRMADA', N'Reserva confirmada', N'Tu reserva en WayPath ha sido confirmada.'),
(N'EMAIL', N'PAGO_APROBADO', N'Pago aprobado', N'Tu pago fue aprobado correctamente.'),
(N'EMAIL', N'TICKET_SOPORTE_CREADO', N'Ticket de soporte creado', N'Hemos recibido tu solicitud de soporte.')
) v(Canal, Codigo, Asunto, Cuerpo) ON c.Codigo = v.Canal;
GO

INSERT INTO Marketing.CampanasPromocionales (EstadoId, Codigo, Nombre, Descripcion, FechaInicio, FechaFin, Presupuesto)
SELECT e.EstadoId, N'VERANO-2026', N'Oferta de Verano 2026', N'Campana para impulsar paquetes de playa y aventura.', CONVERT(DATETIME2(0), '2026-09-01'), CONVERT(DATETIME2(0), '2027-03-31'), 25000.00
FROM Configuracion.Estados e
WHERE e.Grupo = N'CAMPANA' AND e.Codigo = N'ACTIVA';
GO

INSERT INTO Marketing.Descuentos (CampanaPromocionalId, Codigo, Nombre, TipoDescuento, Valor, MontoMaximo)
SELECT c.CampanaPromocionalId, N'DESC-VERANO-10', N'Descuento verano 10%', N'PORCENTAJE', 10.00, 150.00
FROM Marketing.CampanasPromocionales c
WHERE c.Codigo = N'VERANO-2026';
GO

INSERT INTO Marketing.Cupones (DescuentoId, Codigo, UsoMaximo, FechaInicio, FechaFin)
SELECT d.DescuentoId, N'WAYVERANO10', 500, CONVERT(DATETIME2(0), '2026-09-01'), CONVERT(DATETIME2(0), '2027-03-31')
FROM Marketing.Descuentos d
WHERE d.Codigo = N'DESC-VERANO-10';
GO

INSERT INTO Marketing.PaquetePromociones (PaqueteId, DescuentoId)
SELECT p.PaqueteId, d.DescuentoId
FROM Catalogo.Paquetes p
CROSS JOIN Marketing.Descuentos d
WHERE p.Slug IN (N'paquete-mancora', N'paquete-huacachina', N'paquete-islas-ballestas')
AND d.Codigo = N'DESC-VERANO-10';
GO

INSERT INTO Marketing.LandingPages (CampanaPromocionalId, Slug, Titulo, Oferta, EstadoId, PublicadaEn)
SELECT c.CampanaPromocionalId, v.Slug, v.Titulo, v.Oferta, e.EstadoId, SYSUTCDATETIME()
FROM Marketing.CampanasPromocionales c
CROSS JOIN Configuracion.Estados e
CROSS JOIN (VALUES
(N'landing-mancora', N'Escapada a Mancora', N'Reserva paquete de playa desde S/ 350'),
(N'landing-guia', N'Guia del Viajero', N'Descarga gratuita de guia turistica')
) v(Slug, Titulo, Oferta)
WHERE c.Codigo = N'VERANO-2026'
AND e.Grupo = N'LANDING'
AND e.Codigo = N'ACTIVA';
GO

INSERT INTO Reportes.KpiDefiniciones (Codigo, Nombre, Descripcion, Formula)
VALUES
(N'VENTAS_MENSUALES', N'Ventas mensuales', N'Total vendido por mes', N'SUM(Ventas.Compras.TotalPagado)'),
(N'TASA_CONVERSION', N'Tasa de conversion', N'Reservas pagadas sobre visitas', N'Compras / Visitas'),
(N'DESTINOS_MAS_RESERVADOS', N'Destinos mas reservados', N'Ranking de destinos por reservas confirmadas', N'COUNT(Reservas) GROUP BY Destino'),
(N'METODOS_PAGO', N'Metodos de pago preferidos', N'Distribucion de pagos por metodo', N'COUNT(Pagos) GROUP BY MetodoPago'),
(N'LEADS_CAPTURADOS', N'Leads capturados', N'Cantidad de leads por landing o campana', N'COUNT(Marketing.Leads)'),
(N'RESENAS_PROMEDIO', N'Promedio de resenas', N'Calificacion promedio por destino', N'AVG(Ventas.Resenas.Calificacion)');
GO

PRINT 'WayPathDB creada correctamente: 97 tablas, 11 esquemas, claves primarias, claves foraneas, restricciones, indices y datos iniciales.';
GO