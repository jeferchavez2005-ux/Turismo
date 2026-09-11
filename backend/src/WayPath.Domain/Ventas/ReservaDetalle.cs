namespace WayPath.Domain.Ventas;

public sealed class ReservaDetalle
{
    public long ReservaDetalleId { get; set; }
    public long ReservaId { get; set; }
    public long? PaqueteId { get; set; }
    public long? TarifaId { get; set; }
    public string TipoDetalle { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Importe { get; set; }
}
