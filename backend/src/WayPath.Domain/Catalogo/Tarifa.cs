namespace WayPath.Domain.Catalogo;

public sealed class Tarifa
{
    public long TarifaId { get; set; }
    public long PaqueteId { get; set; }
    public int MonedaId { get; set; }
    public int EstadoId { get; set; }
    public string TipoTarifa { get; set; } = "";
    public decimal PrecioBase { get; set; }
    public decimal? PrecioNino { get; set; }
    public DateOnly VigenciaInicio { get; set; }
    public DateOnly? VigenciaFin { get; set; }
    public DateTime CreadoEn { get; set; }
}
