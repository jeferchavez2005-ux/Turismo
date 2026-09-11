using Microsoft.EntityFrameworkCore;
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
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ChangeTracker.Entries<Estado>().Any(e => e.State is not EntityState.Unchanged and not EntityState.Detached))
            throw new InvalidOperationException("Configuracion.Estados is read-only in this module.");
        return base.SaveChangesAsync(cancellationToken);
    }
}

