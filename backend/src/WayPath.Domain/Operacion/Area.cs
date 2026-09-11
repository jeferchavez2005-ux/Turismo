namespace WayPath.Domain.Operacion;

public sealed class Area
{
    public int AreaId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool EsActivo { get; set; }
}
