namespace WayPath.Domain.Configuracion;

// Existing catalog; this module only reads it.
public sealed class Estado
{
    public int EstadoId { get; set; }
    public string Grupo { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public bool EsActivo { get; set; }
}

