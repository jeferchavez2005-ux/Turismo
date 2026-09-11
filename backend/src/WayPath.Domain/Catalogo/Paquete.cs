namespace WayPath.Domain.Catalogo;

public sealed class Paquete
{
    public long PaqueteId { get; set; }
    public long DestinoId { get; set; }
    public int EstadoId { get; set; }
    public string Nombre { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Descripcion { get; set; }
    public int DuracionDias { get; set; }
    public int DuracionNoches { get; set; }
    public string? NivelDificultad { get; set; }
    public bool EsDestacado { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public byte[] FilaVersion { get; set; } = [];
}
