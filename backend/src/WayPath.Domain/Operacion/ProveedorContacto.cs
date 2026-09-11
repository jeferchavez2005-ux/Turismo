namespace WayPath.Domain.Operacion;

public sealed class ProveedorContacto
{
    public long ProveedorContactoId { get; set; }
    public long ProveedorId { get; set; }
    public string Nombres { get; set; } = "";
    public string? Apellidos { get; set; }
    public string? Cargo { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool EsPrincipal { get; set; }
}
