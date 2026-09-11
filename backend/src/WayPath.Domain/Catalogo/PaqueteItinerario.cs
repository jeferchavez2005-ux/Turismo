namespace WayPath.Domain.Catalogo;

public sealed class PaqueteItinerario
{
    public long PaqueteItinerarioId { get; set; }
    public long PaqueteId { get; set; }
    public int DiaNumero { get; set; }
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int Orden { get; set; }
}
