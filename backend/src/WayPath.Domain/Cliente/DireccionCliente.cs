namespace WayPath.Domain.Cliente;

public sealed class DireccionCliente
{
    public long DireccionClienteId { get; set; }
    public long ClienteId { get; set; }
    public int CiudadId { get; set; }
    public string LineaDireccion { get; set; } = "";
    public string? CodigoPostal { get; set; }
    public bool EsPrincipal { get; set; }
}
