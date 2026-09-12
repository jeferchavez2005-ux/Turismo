namespace WayPath.Domain.Reportes;

public sealed class MetricaDiaria
{
    public long MetricaDiariaId { get; set; }
    public int? KpiDefinicionId { get; set; }
    public DateOnly Fecha { get; set; }
    public string NombreMetrica { get; set; } = "";
    public string? DimensionNombre { get; set; }
    public string? DimensionValor { get; set; }
    public decimal ValorDecimal { get; set; }
    public DateTime CreadoEn { get; set; }
}
