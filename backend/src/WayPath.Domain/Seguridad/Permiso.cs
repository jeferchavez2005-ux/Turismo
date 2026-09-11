namespace WayPath.Domain.Seguridad;

public sealed class Permiso
{
    public int PermisoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Modulo { get; set; } = "";
    public string Accion { get; set; } = "";
    public string? Descripcion { get; set; }
}

