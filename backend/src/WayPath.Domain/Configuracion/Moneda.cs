namespace WayPath.Domain.Configuracion;

public sealed class Moneda
{
    public int MonedaId { get; set; }
    public string CodigoIso { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Simbolo { get; set; } = "";
    public bool EsMonedaBase { get; set; }
    public bool EsActivo { get; set; }
}
