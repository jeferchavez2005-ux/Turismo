namespace WayPath.Domain.Operacion;

public sealed class CupoSalida
{
    public long CupoSalidaId { get; set; }
    public long SalidaId { get; set; }
    public string TipoCupo { get; set; } = "";
    public int CupoTotal { get; set; }
    public int CupoReservado { get; set; }
}
