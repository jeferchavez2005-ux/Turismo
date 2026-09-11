namespace WayPath.Domain.Ventas;

public sealed class ComprobanteDetalle
{
    public long ComprobanteDetalleId { get; set; }
    public long ComprobanteId { get; set; }
    public string Descripcion { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Importe { get; set; }
}
