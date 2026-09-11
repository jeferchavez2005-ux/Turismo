namespace WayPath.Domain.Catalogo;

public sealed class Region
{
    public int RegionId { get; set; }
    public int PaisId { get; set; }
    public string Nombre { get; set; } = "";
    public bool EsActivo { get; set; }
}
