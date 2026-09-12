namespace WayPath.Domain.Reportes;

public sealed class KpiDefinicion
{
    public int KpiDefinicionId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? Formula { get; set; }
    public bool EsActivo { get; set; }
}
