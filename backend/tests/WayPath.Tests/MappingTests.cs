using Microsoft.EntityFrameworkCore;
using WayPath.Domain.Catalogo;
using WayPath.Domain.Cliente;
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
        Assert.Equal(17, db.Model.GetEntityTypes().Count());
        Assert.Equal("Usuarios", db.Model.FindEntityType(typeof(Usuario))!.GetTableName());
        Assert.Equal("Seguridad", db.Model.FindEntityType(typeof(Usuario))!.GetSchema());
        Assert.Equal("Clientes", db.Model.FindEntityType(typeof(ClientePerfil))!.GetTableName());
        Assert.Equal("Cliente", db.Model.FindEntityType(typeof(ClientePerfil))!.GetSchema());
        Assert.Equal("DocumentosCliente", db.Model.FindEntityType(typeof(DocumentoCliente))!.GetTableName());
        Assert.Equal("DireccionesCliente", db.Model.FindEntityType(typeof(DireccionCliente))!.GetTableName());
        Assert.Equal("PreferenciasCliente", db.Model.FindEntityType(typeof(PreferenciaCliente))!.GetTableName());
        Assert.Equal("Ciudades", db.Model.FindEntityType(typeof(Ciudad))!.GetTableName());
        Assert.True(db.Model.FindEntityType(typeof(Usuario))!.FindProperty("FilaVersion")!.IsConcurrencyToken);
        Assert.True(db.Model.FindEntityType(typeof(ClientePerfil))!.FindProperty("FilaVersion")!.IsConcurrencyToken);
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
