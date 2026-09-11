namespace WayPath.Domain.Operacion;

public sealed class Salida
{
    public long SalidaId { get; set; }
    public long PaqueteId { get; set; }
    public int EstadoId { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int CupoTotal { get; set; }
    public int CupoReservado { get; set; }
    public string? PuntoEncuentro { get; set; }
    public DateTime CreadoEn { get; set; }
}
