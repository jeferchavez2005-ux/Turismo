namespace WayPath.Domain.Cliente;

public sealed class PreferenciaCliente
{
    public long PreferenciaClienteId { get; set; }
    public long ClienteId { get; set; }
    public string IdiomaPreferido { get; set; } = "es";
    public int? MonedaPreferidaId { get; set; }
    public bool RecibePromociones { get; set; } = true;
    public bool RequiereAccesibilidad { get; set; }
    public string? Observaciones { get; set; }
}
