namespace WayPath.Domain.Catalogo;

public sealed class Destino
{
    public long DestinoId { get; set; }
    public int CiudadId { get; set; }
    public long? UbicacionTuristicaId { get; set; }
    public int EstadoId { get; set; }
    public string Nombre { get; set; } = "";
    public string Slug { get; set; } = "";
    public string DescripcionCorta { get; set; } = "";
    public string? DescripcionLarga { get; set; }
    public int? AltitudMetros { get; set; }
    public bool EsDestacado { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public byte[] FilaVersion { get; set; } = [];
}
