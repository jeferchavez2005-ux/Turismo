namespace WayPath.Domain.Auditoria;

public sealed class ErrorAplicacion
{
    public long ErrorAplicacionId { get; set; }
    public long? UsuarioId { get; set; }
    public string Nivel { get; set; } = "";
    public string Origen { get; set; } = "";
    public string Mensaje { get; set; } = "";
    public string? StackTrace { get; set; }
    public string? DatosJson { get; set; }
    public DateTime CreadoEn { get; set; }
}
