namespace WayPath.Domain.Catalogo;

public sealed class AtraccionDestino
{
    public long AtraccionDestinoId { get; set; }
    public long DestinoId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int? DuracionEstimadaMinutos { get; set; }
    public int Orden { get; set; }
}
