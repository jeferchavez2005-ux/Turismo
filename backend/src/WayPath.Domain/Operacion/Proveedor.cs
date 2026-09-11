namespace WayPath.Domain.Operacion;

public sealed class Proveedor
{
    public long ProveedorId { get; set; }
    public int? TipoDocumentoId { get; set; }
    public string? NumeroDocumento { get; set; }
    public string RazonSocial { get; set; } = "";
    public string? NombreComercial { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public int? CiudadId { get; set; }
    public int EstadoId { get; set; }
    public DateTime CreadoEn { get; set; }
}
