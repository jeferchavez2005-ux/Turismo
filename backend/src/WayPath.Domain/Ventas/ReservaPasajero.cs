namespace WayPath.Domain.Ventas;

public sealed class ReservaPasajero
{
    public long ReservaPasajeroId { get; set; }
    public long ReservaId { get; set; }
    public long PasajeroId { get; set; }
    public string TipoPasajero { get; set; } = "";
    public bool EsTitular { get; set; }
}
