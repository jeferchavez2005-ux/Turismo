namespace WayPath.Application;

public sealed class ReservaService(IReservaStore store)
{
    private static readonly HashSet<string> PassengerTypes = ["ADULTO", "NINO", "INFANTE", "ADULTO_MAYOR"];

    public Task<ReservaDetalleDto> Create(long usuarioId, CrearReservaRequest request, CancellationToken ct) =>
        store.Create(usuarioId, Clean(request), ct);
    public Task<ReservaResumenDto[]> Mine(long usuarioId, CancellationToken ct) => store.Mine(usuarioId, ct);
    public Task<ReservaDetalleDto?> DetailForUser(long usuarioId, long reservaId, CancellationToken ct) =>
        store.DetailForUser(usuarioId, reservaId, ct);
    public Task<ReservaDetalleDto?> DetailAdmin(long reservaId, CancellationToken ct) =>
        store.DetailAdmin(reservaId, ct);
    public Task CancelMine(long usuarioId, long reservaId, CancellationToken ct) =>
        store.CancelMine(usuarioId, reservaId, ct);
    public Task<ReservaDetalleDto> UpdatePassengers(long usuarioId, long reservaId,
        ActualizarPasajerosReservaRequest request, CancellationToken ct) =>
        store.UpdatePassengers(usuarioId, reservaId, Clean(request), ct);
    public Task<ReservaPage> List(ReservaFiltroRequest filtro, int page, int pageSize, CancellationToken ct) =>
        store.List(filtro, page, pageSize, ct);
    public Task ChangeState(long reservaId, CambiarEstadoReservaRequest request, long actorId, CancellationToken ct) =>
        store.ChangeState(reservaId, Clean(request), actorId, ct);
    public Task<PasajeroReservaDto[]> MyPassengers(long usuarioId, CancellationToken ct) =>
        store.MyPassengers(usuarioId, ct);
    public Task<PasajeroReservaDto> CreatePassenger(long usuarioId, PasajeroReservaRequest request, CancellationToken ct) =>
        store.CreatePassenger(usuarioId, Clean(request), ct);

    private static CrearReservaRequest Clean(CrearReservaRequest request) => request with
    {
        Observaciones = Optional(request.Observaciones),
        Pasajeros = CleanPassengers(request.Pasajeros)
    };

    private static ActualizarPasajerosReservaRequest Clean(ActualizarPasajerosReservaRequest request) =>
        request with { Pasajeros = CleanPassengers(request.Pasajeros) };

    private static CambiarEstadoReservaRequest Clean(CambiarEstadoReservaRequest request) => request with
    {
        Codigo = Required(request.Codigo, "El estado es obligatorio.").ToUpperInvariant(),
        Comentario = Optional(request.Comentario)
    };

    private static PasajeroReservaRequest Clean(PasajeroReservaRequest request) => request with
    {
        Nombres = Required(request.Nombres, "Los nombres del pasajero son obligatorios."),
        Apellidos = Required(request.Apellidos, "Los apellidos del pasajero son obligatorios."),
        RequerimientosEspeciales = Optional(request.RequerimientosEspeciales)
    };

    private static ReservaPasajeroRequest[] CleanPassengers(ReservaPasajeroRequest[] passengers)
    {
        if (passengers.Length == 0) throw new SecurityFault(400, "Seleccione al menos un pasajero.");
        if (passengers.Select(x => x.PasajeroId).Distinct().Count() != passengers.Length)
            throw new SecurityFault(400, "No puede repetir pasajeros en la misma reserva.");
        var titularCount = passengers.Count(x => x.EsTitular);
        if (titularCount != 1) throw new SecurityFault(400, "Debe seleccionar exactamente un pasajero titular.");
        return passengers.Select(x =>
        {
            var type = Required(x.TipoPasajero, "El tipo de pasajero es obligatorio.").ToUpperInvariant();
            if (!PassengerTypes.Contains(type)) throw new SecurityFault(400, "Tipo de pasajero no valido.");
            return x with { TipoPasajero = type };
        }).ToArray();
    }

    private static string Required(string value, string error)
    {
        var text = value.Trim();
        if (text.Length == 0) throw new SecurityFault(400, error);
        return text;
    }

    private static string? Optional(string? value)
    {
        var text = value?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
