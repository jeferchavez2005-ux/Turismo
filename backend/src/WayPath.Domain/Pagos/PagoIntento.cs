namespace WayPath.Domain.Pagos;

public sealed class PagoIntento
{
    public long PagoIntentoId { get; set; }
    public long OrdenPagoId { get; set; }
    public long? PagoId { get; set; }
    public int MetodoPagoId { get; set; }
    public string Resultado { get; set; } = "";
    public string? Mensaje { get; set; }
    public string? PayloadRespuesta { get; set; }
    public DateTime CreadoEn { get; set; }
}
