using Microsoft.EntityFrameworkCore;
using WayPath.Domain.Catalogo;
using WayPath.Domain.Cliente;
using WayPath.Domain.Configuracion;
using WayPath.Domain.Seguridad;

namespace WayPath.Infrastructure;

public sealed class WayPathDbContext(DbContextOptions<WayPathDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<SesionUsuario> Sesiones => Set<SesionUsuario>();
    public DbSet<Estado> Estados => Set<Estado>();
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();
    public DbSet<Moneda> Monedas => Set<Moneda>();
    public DbSet<Pais> Paises => Set<Pais>();
    public DbSet<Region> Regiones => Set<Region>();
    public DbSet<Ciudad> Ciudades => Set<Ciudad>();
    public DbSet<UbicacionTuristica> UbicacionesTuristicas => Set<UbicacionTuristica>();
    public DbSet<Destino> Destinos => Set<Destino>();
    public DbSet<CategoriaDestino> CategoriasDestino => Set<CategoriaDestino>();
    public DbSet<DestinoCategoria> DestinoCategorias => Set<DestinoCategoria>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<DestinoImagen> DestinoImagenes => Set<DestinoImagen>();
    public DbSet<AtraccionDestino> AtraccionesDestino => Set<AtraccionDestino>();
    public DbSet<Paquete> Paquetes => Set<Paquete>();
    public DbSet<Tarifa> Tarifas => Set<Tarifa>();
    public DbSet<PaqueteItinerario> PaqueteItinerario => Set<PaqueteItinerario>();
    public DbSet<PaqueteIncluye> PaqueteIncluye => Set<PaqueteIncluye>();
    public DbSet<PaqueteNoIncluye> PaqueteNoIncluye => Set<PaqueteNoIncluye>();
    public DbSet<ClientePerfil> Clientes => Set<ClientePerfil>();
    public DbSet<DocumentoCliente> DocumentosCliente => Set<DocumentoCliente>();
    public DbSet<DireccionCliente> DireccionesCliente => Set<DireccionCliente>();
    public DbSet<PreferenciaCliente> PreferenciasCliente => Set<PreferenciaCliente>();
    public DbSet<Pasajero> Pasajeros => Set<Pasajero>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var u = model.Entity<Usuario>();
        u.ToTable("Usuarios", "Seguridad", t => t.ExcludeFromMigrations());
        u.HasKey(x => x.UsuarioId);
        u.Property(x => x.UsuarioId).UseIdentityColumn();
        u.Property(x => x.Email).HasMaxLength(255).IsRequired();
        u.HasIndex(x => x.Email).IsUnique();
        u.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        u.Property(x => x.NombreUsuario).HasMaxLength(100);
        u.Property(x => x.FilaVersion).IsRowVersion();
        u.Property(x => x.CreadoEn).HasColumnType("datetime2(0)").HasDefaultValueSql("sysutcdatetime()");
        u.Property(x => x.ActualizadoEn).HasColumnType("datetime2(0)");
        u.Property(x => x.UltimoAccesoEn).HasColumnType("datetime2(0)");
        u.HasOne<Estado>().WithMany().HasForeignKey(x => x.EstadoId).OnDelete(DeleteBehavior.NoAction);

        var r = model.Entity<Rol>();
        r.ToTable("Roles", "Seguridad", t => t.ExcludeFromMigrations());
        r.HasKey(x => x.RolId);
        r.Property(x => x.RolId).UseIdentityColumn();
        r.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
        r.HasIndex(x => x.Codigo).IsUnique();
        r.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        r.Property(x => x.Descripcion).HasMaxLength(250);

        var p = model.Entity<Permiso>();
        p.ToTable("Permisos", "Seguridad", t => t.ExcludeFromMigrations());
        p.HasKey(x => x.PermisoId);
        p.Property(x => x.PermisoId).UseIdentityColumn();
        p.Property(x => x.Codigo).HasMaxLength(100).IsRequired();
        p.HasIndex(x => x.Codigo).IsUnique();
        p.Property(x => x.Modulo).HasMaxLength(80).IsRequired();
        p.Property(x => x.Accion).HasMaxLength(80).IsRequired();
        p.Property(x => x.Descripcion).HasMaxLength(250);

        var ur = model.Entity<UsuarioRol>();
        ur.ToTable("UsuarioRoles", "Seguridad", t => t.ExcludeFromMigrations());
        ur.HasKey(x => new { x.UsuarioId, x.RolId });
        ur.Property(x => x.AsignadoEn).HasColumnType("datetime2(0)").HasDefaultValueSql("sysutcdatetime()");
        ur.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
        ur.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AsignadoPorUsuarioId).OnDelete(DeleteBehavior.NoAction);
        ur.HasOne<Rol>().WithMany().HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.NoAction);

        var rp = model.Entity<RolPermiso>();
        rp.ToTable("RolPermisos", "Seguridad", t => t.ExcludeFromMigrations());
        rp.HasKey(x => new { x.RolId, x.PermisoId });
        rp.HasOne<Rol>().WithMany().HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.NoAction);
        rp.HasOne<Permiso>().WithMany().HasForeignKey(x => x.PermisoId).OnDelete(DeleteBehavior.NoAction);

        var s = model.Entity<SesionUsuario>();
        s.ToTable("SesionesUsuario", "Seguridad", t => t.ExcludeFromMigrations());
        s.HasKey(x => x.SesionUsuarioId);
        s.Property(x => x.SesionUsuarioId).UseIdentityColumn();
        s.Property(x => x.IdentificadorSesion).HasMaxLength(120).IsRequired();
        s.HasIndex(x => x.IdentificadorSesion).IsUnique();
        s.Property(x => x.Ip).HasMaxLength(45);
        s.Property(x => x.UserAgent).HasMaxLength(500);
        s.Property(x => x.IniciadaEn).HasColumnType("datetime2(0)");
        s.Property(x => x.FinalizadaEn).HasColumnType("datetime2(0)");
        s.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);

        var e = model.Entity<Estado>();
        e.ToTable("Estados", "Configuracion", t => t.ExcludeFromMigrations());
        e.HasKey(x => x.EstadoId);
        e.Property(x => x.EstadoId).UseIdentityColumn();
        e.Property(x => x.Grupo).HasMaxLength(50).IsRequired();
        e.Property(x => x.Codigo).HasMaxLength(60).IsRequired();
        e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();

        var td = model.Entity<TipoDocumento>();
        td.ToTable("TiposDocumento", "Configuracion", t => t.ExcludeFromMigrations());
        td.HasKey(x => x.TipoDocumentoId);
        td.Property(x => x.TipoDocumentoId).UseIdentityColumn();
        td.Property(x => x.Codigo).HasMaxLength(30).IsRequired();
        td.Property(x => x.Nombre).HasMaxLength(120).IsRequired();

        var m = model.Entity<Moneda>();
        m.ToTable("Monedas", "Configuracion", t => t.ExcludeFromMigrations());
        m.HasKey(x => x.MonedaId);
        m.Property(x => x.MonedaId).UseIdentityColumn();
        m.Property(x => x.CodigoIso).HasMaxLength(3).IsRequired();
        m.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
        m.Property(x => x.Simbolo).HasMaxLength(10).IsRequired();

        var pais = model.Entity<Pais>();
        pais.ToTable("Paises", "Catalogo", t => t.ExcludeFromMigrations());
        pais.HasKey(x => x.PaisId);
        pais.Property(x => x.PaisId).UseIdentityColumn();
        pais.Property(x => x.CodigoIso2).HasMaxLength(2).IsRequired();
        pais.Property(x => x.Nombre).HasMaxLength(120).IsRequired();

        var region = model.Entity<Region>();
        region.ToTable("Regiones", "Catalogo", t => t.ExcludeFromMigrations());
        region.HasKey(x => x.RegionId);
        region.Property(x => x.RegionId).UseIdentityColumn();
        region.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        region.HasOne<Pais>().WithMany().HasForeignKey(x => x.PaisId).OnDelete(DeleteBehavior.NoAction);

        var ciudad = model.Entity<Ciudad>();
        ciudad.ToTable("Ciudades", "Catalogo", t => t.ExcludeFromMigrations());
        ciudad.HasKey(x => x.CiudadId);
        ciudad.Property(x => x.CiudadId).UseIdentityColumn();
        ciudad.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        ciudad.HasOne<Region>().WithMany().HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.NoAction);

        var ubicacion = model.Entity<UbicacionTuristica>();
        ubicacion.ToTable("UbicacionesTuristicas", "Catalogo", t => t.ExcludeFromMigrations());
        ubicacion.HasKey(x => x.UbicacionTuristicaId);
        ubicacion.Property(x => x.UbicacionTuristicaId).UseIdentityColumn();
        ubicacion.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        ubicacion.Property(x => x.Direccion).HasMaxLength(250);
        ubicacion.Property(x => x.Latitud).HasColumnType("decimal(9,6)");
        ubicacion.Property(x => x.Longitud).HasColumnType("decimal(9,6)");
        ubicacion.HasOne<Ciudad>().WithMany().HasForeignKey(x => x.CiudadId).OnDelete(DeleteBehavior.NoAction);

        var destino = model.Entity<Destino>();
        destino.ToTable("Destinos", "Catalogo", t => t.ExcludeFromMigrations());
        destino.HasKey(x => x.DestinoId);
        destino.Property(x => x.DestinoId).UseIdentityColumn();
        destino.Property(x => x.Nombre).HasMaxLength(160).IsRequired();
        destino.Property(x => x.Slug).HasMaxLength(180).IsRequired();
        destino.HasIndex(x => x.Slug).IsUnique();
        destino.Property(x => x.DescripcionCorta).HasMaxLength(300).IsRequired();
        destino.Property(x => x.DescripcionLarga).HasColumnType("nvarchar(max)");
        destino.Property(x => x.CreadoEn).HasColumnType("datetime2(0)").HasDefaultValueSql("sysutcdatetime()");
        destino.Property(x => x.ActualizadoEn).HasColumnType("datetime2(0)");
        destino.Property(x => x.FilaVersion).IsRowVersion();
        destino.HasOne<Ciudad>().WithMany().HasForeignKey(x => x.CiudadId).OnDelete(DeleteBehavior.NoAction);
        destino.HasOne<UbicacionTuristica>().WithMany().HasForeignKey(x => x.UbicacionTuristicaId).OnDelete(DeleteBehavior.NoAction);
        destino.HasOne<Estado>().WithMany().HasForeignKey(x => x.EstadoId).OnDelete(DeleteBehavior.NoAction);

        var categoria = model.Entity<CategoriaDestino>();
        categoria.ToTable("CategoriasDestino", "Catalogo", t => t.ExcludeFromMigrations());
        categoria.HasKey(x => x.CategoriaDestinoId);
        categoria.Property(x => x.CategoriaDestinoId).UseIdentityColumn();
        categoria.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
        categoria.HasIndex(x => x.Codigo).IsUnique();
        categoria.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        categoria.Property(x => x.Descripcion).HasMaxLength(250);

        var destinoCategoria = model.Entity<DestinoCategoria>();
        destinoCategoria.ToTable("DestinoCategorias", "Catalogo", t => t.ExcludeFromMigrations());
        destinoCategoria.HasKey(x => new { x.DestinoId, x.CategoriaDestinoId });
        destinoCategoria.HasOne<Destino>().WithMany().HasForeignKey(x => x.DestinoId).OnDelete(DeleteBehavior.NoAction);
        destinoCategoria.HasOne<CategoriaDestino>().WithMany().HasForeignKey(x => x.CategoriaDestinoId).OnDelete(DeleteBehavior.NoAction);

        var media = model.Entity<MediaAsset>();
        media.ToTable("MediaAssets", "Catalogo", t => t.ExcludeFromMigrations());
        media.HasKey(x => x.MediaAssetId);
        media.Property(x => x.MediaAssetId).UseIdentityColumn();
        media.Property(x => x.Tipo).HasMaxLength(30).IsRequired();
        media.Property(x => x.Url).HasMaxLength(1000).IsRequired();
        media.Property(x => x.NombreArchivo).HasMaxLength(255);
        media.Property(x => x.MimeType).HasMaxLength(100);
        media.Property(x => x.TextoAlternativo).HasMaxLength(250);
        media.Property(x => x.CreadoEn).HasColumnType("datetime2(0)").HasDefaultValueSql("sysutcdatetime()");

        var destinoImagen = model.Entity<DestinoImagen>();
        destinoImagen.ToTable("DestinoImagenes", "Catalogo", t => t.ExcludeFromMigrations());
        destinoImagen.HasKey(x => x.DestinoImagenId);
        destinoImagen.Property(x => x.DestinoImagenId).UseIdentityColumn();
        destinoImagen.HasOne<Destino>().WithMany().HasForeignKey(x => x.DestinoId).OnDelete(DeleteBehavior.NoAction);
        destinoImagen.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.NoAction);

        var atraccion = model.Entity<AtraccionDestino>();
        atraccion.ToTable("AtraccionesDestino", "Catalogo", t => t.ExcludeFromMigrations());
        atraccion.HasKey(x => x.AtraccionDestinoId);
        atraccion.Property(x => x.AtraccionDestinoId).UseIdentityColumn();
        atraccion.Property(x => x.Nombre).HasMaxLength(160).IsRequired();
        atraccion.Property(x => x.Descripcion).HasMaxLength(500);
        atraccion.HasOne<Destino>().WithMany().HasForeignKey(x => x.DestinoId).OnDelete(DeleteBehavior.NoAction);

        var paquete = model.Entity<Paquete>();
        paquete.ToTable("Paquetes", "Catalogo", t => t.ExcludeFromMigrations());
        paquete.HasKey(x => x.PaqueteId);
        paquete.Property(x => x.PaqueteId).UseIdentityColumn();
        paquete.Property(x => x.Nombre).HasMaxLength(180).IsRequired();
        paquete.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        paquete.HasIndex(x => x.Slug).IsUnique();
        paquete.Property(x => x.Descripcion).HasColumnType("nvarchar(max)");
        paquete.Property(x => x.NivelDificultad).HasMaxLength(30);
        paquete.Property(x => x.CreadoEn).HasColumnType("datetime2(0)").HasDefaultValueSql("sysutcdatetime()");
        paquete.Property(x => x.ActualizadoEn).HasColumnType("datetime2(0)");
        paquete.Property(x => x.FilaVersion).IsRowVersion();
        paquete.HasOne<Destino>().WithMany().HasForeignKey(x => x.DestinoId).OnDelete(DeleteBehavior.NoAction);
        paquete.HasOne<Estado>().WithMany().HasForeignKey(x => x.EstadoId).OnDelete(DeleteBehavior.NoAction);

        var tarifa = model.Entity<Tarifa>();
        tarifa.ToTable("Tarifas", "Catalogo", t => t.ExcludeFromMigrations());
        tarifa.HasKey(x => x.TarifaId);
        tarifa.Property(x => x.TarifaId).UseIdentityColumn();
        tarifa.Property(x => x.TipoTarifa).HasMaxLength(40).IsRequired();
        tarifa.Property(x => x.PrecioBase).HasColumnType("decimal(18,2)");
        tarifa.Property(x => x.PrecioNino).HasColumnType("decimal(18,2)");
        tarifa.Property(x => x.VigenciaInicio).HasColumnType("date");
        tarifa.Property(x => x.VigenciaFin).HasColumnType("date");
        tarifa.Property(x => x.CreadoEn).HasColumnType("datetime2(0)").HasDefaultValueSql("sysutcdatetime()");
        tarifa.HasOne<Paquete>().WithMany().HasForeignKey(x => x.PaqueteId).OnDelete(DeleteBehavior.NoAction);
        tarifa.HasOne<Moneda>().WithMany().HasForeignKey(x => x.MonedaId).OnDelete(DeleteBehavior.NoAction);
        tarifa.HasOne<Estado>().WithMany().HasForeignKey(x => x.EstadoId).OnDelete(DeleteBehavior.NoAction);

        var itin = model.Entity<PaqueteItinerario>();
        itin.ToTable("PaqueteItinerario", "Catalogo", t => t.ExcludeFromMigrations());
        itin.HasKey(x => x.PaqueteItinerarioId);
        itin.Property(x => x.PaqueteItinerarioId).UseIdentityColumn();
        itin.Property(x => x.Titulo).HasMaxLength(160).IsRequired();
        itin.Property(x => x.Descripcion).HasColumnType("nvarchar(max)");
        itin.HasOne<Paquete>().WithMany().HasForeignKey(x => x.PaqueteId).OnDelete(DeleteBehavior.NoAction);

        var incluye = model.Entity<PaqueteIncluye>();
        incluye.ToTable("PaqueteIncluye", "Catalogo", t => t.ExcludeFromMigrations());
        incluye.HasKey(x => x.PaqueteIncluyeId);
        incluye.Property(x => x.PaqueteIncluyeId).UseIdentityColumn();
        incluye.Property(x => x.Descripcion).HasMaxLength(250).IsRequired();
        incluye.HasOne<Paquete>().WithMany().HasForeignKey(x => x.PaqueteId).OnDelete(DeleteBehavior.NoAction);

        var noIncluye = model.Entity<PaqueteNoIncluye>();
        noIncluye.ToTable("PaqueteNoIncluye", "Catalogo", t => t.ExcludeFromMigrations());
        noIncluye.HasKey(x => x.PaqueteNoIncluyeId);
        noIncluye.Property(x => x.PaqueteNoIncluyeId).UseIdentityColumn();
        noIncluye.Property(x => x.Descripcion).HasMaxLength(250).IsRequired();
        noIncluye.HasOne<Paquete>().WithMany().HasForeignKey(x => x.PaqueteId).OnDelete(DeleteBehavior.NoAction);

        var cliente = model.Entity<ClientePerfil>();
        cliente.ToTable("Clientes", "Cliente", t => t.ExcludeFromMigrations());
        cliente.HasKey(x => x.ClienteId);
        cliente.Property(x => x.ClienteId).UseIdentityColumn();
        cliente.HasIndex(x => x.UsuarioId).IsUnique();
        cliente.Property(x => x.Nombres).HasMaxLength(120).IsRequired();
        cliente.Property(x => x.Apellidos).HasMaxLength(120).IsRequired();
        cliente.Property(x => x.Telefono).HasMaxLength(30);
        cliente.Property(x => x.FechaNacimiento).HasColumnType("date");
        cliente.Property(x => x.Genero).HasMaxLength(20);
        cliente.Property(x => x.CreadoEn).HasColumnType("datetime2(0)").HasDefaultValueSql("sysutcdatetime()");
        cliente.Property(x => x.ActualizadoEn).HasColumnType("datetime2(0)");
        cliente.Property(x => x.FilaVersion).IsRowVersion();
        cliente.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.NoAction);
        cliente.HasOne<Estado>().WithMany().HasForeignKey(x => x.EstadoId).OnDelete(DeleteBehavior.NoAction);

        var doc = model.Entity<DocumentoCliente>();
        doc.ToTable("DocumentosCliente", "Cliente", t => t.ExcludeFromMigrations());
        doc.HasKey(x => x.DocumentoClienteId);
        doc.Property(x => x.DocumentoClienteId).UseIdentityColumn();
        doc.Property(x => x.NumeroDocumento).HasMaxLength(40).IsRequired();
        doc.Property(x => x.FechaVencimiento).HasColumnType("date");
        doc.HasIndex(x => new { x.TipoDocumentoId, x.NumeroDocumento }).IsUnique();
        doc.HasOne<ClientePerfil>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
        doc.HasOne<TipoDocumento>().WithMany().HasForeignKey(x => x.TipoDocumentoId).OnDelete(DeleteBehavior.NoAction);
        doc.HasOne<Pais>().WithMany().HasForeignKey(x => x.PaisEmisionId).OnDelete(DeleteBehavior.NoAction);

        var dir = model.Entity<DireccionCliente>();
        dir.ToTable("DireccionesCliente", "Cliente", t => t.ExcludeFromMigrations());
        dir.HasKey(x => x.DireccionClienteId);
        dir.Property(x => x.DireccionClienteId).UseIdentityColumn();
        dir.Property(x => x.LineaDireccion).HasMaxLength(250).IsRequired();
        dir.Property(x => x.CodigoPostal).HasMaxLength(20);
        dir.HasOne<ClientePerfil>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
        dir.HasOne<Ciudad>().WithMany().HasForeignKey(x => x.CiudadId).OnDelete(DeleteBehavior.NoAction);

        var pref = model.Entity<PreferenciaCliente>();
        pref.ToTable("PreferenciasCliente", "Cliente", t => t.ExcludeFromMigrations());
        pref.HasKey(x => x.PreferenciaClienteId);
        pref.Property(x => x.PreferenciaClienteId).UseIdentityColumn();
        pref.HasIndex(x => x.ClienteId).IsUnique();
        pref.Property(x => x.IdiomaPreferido).HasMaxLength(10).IsRequired();
        pref.Property(x => x.Observaciones).HasMaxLength(500);
        pref.HasOne<ClientePerfil>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
        pref.HasOne<Moneda>().WithMany().HasForeignKey(x => x.MonedaPreferidaId).OnDelete(DeleteBehavior.NoAction);

        var pas = model.Entity<Pasajero>();
        pas.ToTable("Pasajeros", "Cliente", t => t.ExcludeFromMigrations());
        pas.HasKey(x => x.PasajeroId);
        pas.Property(x => x.PasajeroId).UseIdentityColumn();
        pas.Property(x => x.NumeroDocumento).HasMaxLength(40);
        pas.Property(x => x.Nombres).HasMaxLength(120).IsRequired();
        pas.Property(x => x.Apellidos).HasMaxLength(120).IsRequired();
        pas.Property(x => x.FechaNacimiento).HasColumnType("date");
        pas.Property(x => x.RequerimientosEspeciales).HasMaxLength(500);
        pas.Property(x => x.CreadoEn).HasColumnType("datetime2(0)").HasDefaultValueSql("sysutcdatetime()");
        pas.HasOne<ClientePerfil>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.NoAction);
        pas.HasOne<TipoDocumento>().WithMany().HasForeignKey(x => x.TipoDocumentoId).OnDelete(DeleteBehavior.NoAction);
        pas.HasOne<Pais>().WithMany().HasForeignKey(x => x.NacionalidadPaisId).OnDelete(DeleteBehavior.NoAction);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var catalogChanges = ChangeTracker.Entries()
            .Where(e => e.State is not EntityState.Unchanged and not EntityState.Detached)
            .Where(e => e.Entity is Estado or TipoDocumento or Moneda or Pais or Region or Ciudad);
        if (catalogChanges.Any())
            throw new InvalidOperationException("Configuration and location catalogs are read-only in this module.");
        return base.SaveChangesAsync(cancellationToken);
    }
}
