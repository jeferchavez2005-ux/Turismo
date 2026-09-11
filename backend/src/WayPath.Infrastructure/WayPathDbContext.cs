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
