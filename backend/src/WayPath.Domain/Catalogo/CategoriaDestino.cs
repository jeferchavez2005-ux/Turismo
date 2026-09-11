namespace WayPath.Domain.Catalogo;

public sealed class CategoriaDestino
{
    public int CategoriaDestinoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool EsActivo { get; set; }
}
