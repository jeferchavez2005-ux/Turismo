namespace WayPath.Domain.Ventas;

public sealed class Compra
{
    public long CompraId { get; set; }
    public long ReservaId { get; set; }
    public long ClienteId { get; set; }
    public long PagoId { get; set; }
    public int EstadoId { get; set; }
    public string CodigoCompra { get; set; } = "";
    public DateTime FechaCompra { get; set; }
    public decimal TotalPagado { get; set; }
}
