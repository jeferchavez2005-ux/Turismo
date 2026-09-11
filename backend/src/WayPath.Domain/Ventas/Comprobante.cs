namespace WayPath.Domain.Ventas;

public sealed class Comprobante
{
    public long ComprobanteId { get; set; }
    public long CompraId { get; set; }
    public long ClienteId { get; set; }
    public int MonedaId { get; set; }
    public int EstadoId { get; set; }
    public string TipoComprobante { get; set; } = "";
    public string Serie { get; set; } = "";
    public string Numero { get; set; } = "";
    public DateTime FechaEmision { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Total { get; set; }
}
