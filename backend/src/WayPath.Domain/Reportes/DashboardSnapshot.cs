namespace WayPath.Domain.Reportes;

public sealed class DashboardSnapshot
{
    public long DashboardSnapshotId { get; set; }
    public string CodigoDashboard { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string DatosJson { get; set; } = "";
    public long? GeneradoPorUsuarioId { get; set; }
    public DateTime GeneradoEn { get; set; }
}
