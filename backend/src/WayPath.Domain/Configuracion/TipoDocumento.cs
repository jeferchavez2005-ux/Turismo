namespace WayPath.Domain.Configuracion;

public sealed class TipoDocumento
{
    public int TipoDocumentoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int? LongitudMinima { get; set; }
    public int? LongitudMaxima { get; set; }
    public bool EsActivo { get; set; }
}
