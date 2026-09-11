namespace WayPath.Domain.Operacion;

public sealed class ProveedorServicio
{
    public long ProveedorServicioId { get; set; }
    public long ProveedorId { get; set; }
    public int MonedaId { get; set; }
    public int EstadoId { get; set; }
    public string TipoServicio { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal CostoBase { get; set; }
}
