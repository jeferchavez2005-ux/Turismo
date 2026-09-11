namespace WayPath.Domain.Pagos;

public sealed class OrdenPago
{
    public long OrdenPagoId { get; set; }
    public long ReservaId { get; set; }
    public int MonedaId { get; set; }
    public int EstadoId { get; set; }
    public string CodigoOrden { get; set; } = "";
    public decimal MontoTotal { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaExpiracion { get; set; }
}
