namespace WayPath.Domain.Ventas;

public sealed class TicketViaje
{
    public long TicketViajeId { get; set; }
    public long CompraId { get; set; }
    public long ReservaId { get; set; }
    public long PasajeroId { get; set; }
    public long SalidaId { get; set; }
    public int EstadoId { get; set; }
    public string CodigoTicket { get; set; } = "";
    public DateTime EmitidoEn { get; set; }
    public string? UrlDocumento { get; set; }
}
