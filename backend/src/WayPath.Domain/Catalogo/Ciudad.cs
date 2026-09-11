namespace WayPath.Domain.Catalogo;

public sealed class Ciudad
{
    public int CiudadId { get; set; }
    public int RegionId { get; set; }
    public string Nombre { get; set; } = "";
    public bool EsActivo { get; set; }
}
