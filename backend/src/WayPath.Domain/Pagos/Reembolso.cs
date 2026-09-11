namespace WayPath.Domain.Pagos;

public sealed class Reembolso
{
    public long ReembolsoId { get; set; }
    public long PagoId { get; set; }
    public int MonedaId { get; set; }
    public int EstadoId { get; set; }
    public string CodigoReembolso { get; set; } = "";
    public decimal Monto { get; set; }
    public string Motivo { get; set; } = "";
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaProcesado { get; set; }
}
