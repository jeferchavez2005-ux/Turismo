namespace WayPath.Domain.Ventas;

public sealed class Reserva
{
    public long ReservaId { get; set; }
    public string CodigoReserva { get; set; } = "";
    public long ClienteId { get; set; }
    public long SalidaId { get; set; }
    public int EstadoId { get; set; }
    public int MonedaId { get; set; }
    public DateTime FechaReserva { get; set; }
    public DateTime? FechaExpiracion { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public string? Observaciones { get; set; }
    public byte[] FilaVersion { get; set; } = [];
}
