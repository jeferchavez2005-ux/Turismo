namespace WayPath.Domain.Seguridad;

public sealed class Rol
{
    public int RolId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool EsSistema { get; set; }
    public bool EsActivo { get; set; }
}

