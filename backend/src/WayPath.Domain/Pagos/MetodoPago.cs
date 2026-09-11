namespace WayPath.Domain.Pagos;

public sealed class MetodoPago
{
    public int MetodoPagoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public bool RequierePasarela { get; set; }
    public bool EsActivo { get; set; }
}
