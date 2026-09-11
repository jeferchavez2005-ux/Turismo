namespace WayPath.Domain.Operacion;

public sealed class ItinerarioOperativo
{
    public long ItinerarioOperativoId { get; set; }
    public long SalidaId { get; set; }
    public int DiaNumero { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public string Actividad { get; set; } = "";
    public string? Descripcion { get; set; }
}
