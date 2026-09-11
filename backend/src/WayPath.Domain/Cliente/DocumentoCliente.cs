namespace WayPath.Domain.Cliente;

public sealed class DocumentoCliente
{
    public long DocumentoClienteId { get; set; }
    public long ClienteId { get; set; }
    public int TipoDocumentoId { get; set; }
    public string NumeroDocumento { get; set; } = "";
    public int? PaisEmisionId { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public bool EsPrincipal { get; set; }
}
