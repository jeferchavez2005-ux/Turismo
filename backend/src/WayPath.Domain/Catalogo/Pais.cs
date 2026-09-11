namespace WayPath.Domain.Catalogo;

public sealed class Pais
{
    public int PaisId { get; set; }
    public string CodigoIso2 { get; set; } = "";
    public string Nombre { get; set; } = "";
    public bool EsActivo { get; set; }
}
