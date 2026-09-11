namespace WayPath.Domain.Pagos;

public sealed class Pago
{
    public long PagoId { get; set; }
    public long OrdenPagoId { get; set; }
    public long ReservaId { get; set; }
    public int MetodoPagoId { get; set; }
    public int MonedaId { get; set; }
    public int EstadoId { get; set; }
    public string? CodigoTransaccion { get; set; }
    public decimal Monto { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? ReferenciaPasarela { get; set; }
    public string? Observaciones { get; set; }
    public DateTime CreadoEn { get; set; }
    public byte[] FilaVersion { get; set; } = [];
}
