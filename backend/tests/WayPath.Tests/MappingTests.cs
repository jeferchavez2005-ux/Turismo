using Microsoft.EntityFrameworkCore;
using WayPath.Domain.Configuracion;
using WayPath.Domain.Seguridad;
using WayPath.Infrastructure;

namespace WayPath.Tests;

public sealed class MappingTests
{
    [Fact]
    public void Mapping_matches_existing_plural_tables_without_identity_tables()
    {
        using var db = new WayPathDbContext(new DbContextOptionsBuilder<WayPathDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=True").Options);
        Assert.Equal(7, db.Model.GetEntityTypes().Count());
        Assert.Equal("Usuarios", db.Model.FindEntityType(typeof(Usuario))!.GetTableName());
        Assert.Equal("Seguridad", db.Model.FindEntityType(typeof(Usuario))!.GetSchema());
        Assert.True(db.Model.FindEntityType(typeof(Usuario))!.FindProperty("FilaVersion")!.IsConcurrencyToken);
        Assert.Equal(["UsuarioId", "RolId"], db.Model.FindEntityType(typeof(UsuarioRol))!
            .FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray());
        Assert.Equal(["RolId", "PermisoId"], db.Model.FindEntityType(typeof(RolPermiso))!
            .FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray());
        Assert.All(db.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()),
            fk => Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior));
    }

    [Fact]
    public async Task State_catalog_cannot_be_changed_by_this_context()
    {
        await using var db = new WayPathDbContext(new DbContextOptionsBuilder<WayPathDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=True").Options);
        db.Estados.Add(new Estado { Codigo = "NEW", Grupo = "USUARIO", Nombre = "Not allowed" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}

