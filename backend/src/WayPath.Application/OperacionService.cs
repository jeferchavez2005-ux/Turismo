namespace WayPath.Application;

public sealed class OperacionService(IOperacionStore store)
{
    public Task<DisponibilidadDto[]> Disponibilidad(DisponibilidadFiltroRequest filtro, CancellationToken ct) =>
        store.Disponibilidad(filtro, ct);
    public Task<SalidaDto[]> Salidas(SalidasFiltroRequest filtro, CancellationToken ct) =>
        store.ListSalidas(filtro, ct);
    public Task<SalidaDto?> Salida(long id, CancellationToken ct) => store.GetSalida(id, ct);
    public Task<CupoSalidaDto[]> Cupos(long salidaId, CancellationToken ct) => store.ListCupos(salidaId, ct);
    public Task<ProveedorDto[]> Proveedores(CancellationToken ct) => store.ListProveedores(ct);
    public Task<ProveedorServicioDto[]> ProveedorServicios(long? proveedorId, CancellationToken ct) =>
        store.ListProveedorServicios(proveedorId, ct);
    public Task<RecursoOperativoDto[]> Recursos(CancellationToken ct) => store.Recursos(ct);
    public Task<AsignacionOperacionDto[]> Asignaciones(long salidaId, CancellationToken ct) =>
        store.ListAsignaciones(salidaId, ct);
    public Task<BloqueoDisponibilidadDto[]> Bloqueos(long? paqueteId, long? salidaId, CancellationToken ct) =>
        store.ListBloqueos(paqueteId, salidaId, ct);
    public Task<ItinerarioOperativoDto[]> Itinerario(long salidaId, CancellationToken ct) =>
        store.ListItinerario(salidaId, ct);
    public Task<OperacionOpcionesDto> Options(CancellationToken ct) => store.Options(ct);

    public Task<SalidaDto> CreateSalida(SalidaRequest request, CancellationToken ct) =>
        store.CreateSalida(Clean(request), ct);
    public Task<SalidaDto> UpdateSalida(long id, SalidaRequest request, CancellationToken ct) =>
        store.UpdateSalida(id, Clean(request), ct);
    public Task SetSalidaState(long id, string codigo, CancellationToken ct) =>
        store.SetSalidaState(id, Required(codigo, "El estado es obligatorio.").ToUpperInvariant(), ct);
    public Task<CupoSalidaDto> UpsertCupo(long salidaId, CupoSalidaRequest request, CancellationToken ct) =>
        store.UpsertCupo(salidaId, Clean(request), ct);
    public Task<ProveedorDto> CreateProveedor(ProveedorRequest request, CancellationToken ct) =>
        store.CreateProveedor(Clean(request), ct);
    public Task<ProveedorDto> UpdateProveedor(long id, ProveedorRequest request, CancellationToken ct) =>
        store.UpdateProveedor(id, Clean(request), ct);
    public Task SetProveedorState(long id, string codigo, CancellationToken ct) =>
        store.SetProveedorState(id, Required(codigo, "El estado es obligatorio.").ToUpperInvariant(), ct);
    public Task<ProveedorServicioDto> CreateProveedorServicio(ProveedorServicioRequest request, CancellationToken ct) =>
        store.CreateProveedorServicio(Clean(request), ct);
    public Task<ProveedorServicioDto> UpdateProveedorServicio(long id, ProveedorServicioRequest request, CancellationToken ct) =>
        store.UpdateProveedorServicio(id, Clean(request), ct);
    public Task<AsignacionOperacionDto> CreateAsignacion(AsignacionOperacionRequest request, CancellationToken ct) =>
        store.CreateAsignacion(Clean(request), ct);
    public Task<BloqueoDisponibilidadDto> CreateBloqueo(BloqueoDisponibilidadRequest request, long? actorId, CancellationToken ct) =>
        store.CreateBloqueo(Clean(request), actorId, ct);
    public Task<ItinerarioOperativoDto> CreateItinerario(ItinerarioOperativoRequest request, CancellationToken ct) =>
        store.CreateItinerario(Clean(request), ct);

    private static SalidaRequest Clean(SalidaRequest request)
    {
        if (request.FechaFin < request.FechaInicio)
            throw new SecurityFault(400, "La fecha final de la salida no puede ser menor a la fecha inicial.");
        if (request.CupoReservado > request.CupoTotal)
            throw new SecurityFault(400, "El cupo reservado no puede superar el cupo total.");
        return request with { PuntoEncuentro = Optional(request.PuntoEncuentro) };
    }

    private static CupoSalidaRequest Clean(CupoSalidaRequest request)
    {
        if (request.CupoReservado > request.CupoTotal)
            throw new SecurityFault(400, "El cupo reservado no puede superar el cupo total.");
        return request with { TipoCupo = Required(request.TipoCupo, "El tipo de cupo es obligatorio.").ToUpperInvariant() };
    }

    private static ProveedorRequest Clean(ProveedorRequest request) => request with
    {
        NumeroDocumento = Optional(request.NumeroDocumento),
        RazonSocial = Required(request.RazonSocial, "La razon social es obligatoria."),
        NombreComercial = Optional(request.NombreComercial),
        Email = Optional(request.Email)?.ToLowerInvariant(),
        Telefono = Optional(request.Telefono)
    };

    private static ProveedorServicioRequest Clean(ProveedorServicioRequest request) => request with
    {
        TipoServicio = Required(request.TipoServicio, "El tipo de servicio es obligatorio.").ToUpperInvariant(),
        Nombre = Required(request.Nombre, "El nombre del servicio es obligatorio."),
        Descripcion = Optional(request.Descripcion)
    };

    private static AsignacionOperacionRequest Clean(AsignacionOperacionRequest request)
    {
        if (request.ProveedorServicioId is null && request.GuiaId is null && request.TransporteId is null
            && request.HotelId is null && request.HabitacionId is null)
            throw new SecurityFault(400, "Debe seleccionar al menos un recurso operativo.");
        return request with { Observaciones = Optional(request.Observaciones) };
    }

    private static BloqueoDisponibilidadRequest Clean(BloqueoDisponibilidadRequest request)
    {
        if (request.PaqueteId is null && request.SalidaId is null)
            throw new SecurityFault(400, "El bloqueo debe pertenecer a un paquete o salida.");
        if (request.FechaFin < request.FechaInicio)
            throw new SecurityFault(400, "La fecha final del bloqueo no puede ser menor a la fecha inicial.");
        return request with { Motivo = Required(request.Motivo, "El motivo es obligatorio.") };
    }

    private static ItinerarioOperativoRequest Clean(ItinerarioOperativoRequest request)
    {
        if (request.HoraFin is not null && request.HoraInicio is not null && request.HoraFin < request.HoraInicio)
            throw new SecurityFault(400, "La hora final no puede ser menor a la hora inicial.");
        return request with
        {
            Actividad = Required(request.Actividad, "La actividad es obligatoria."),
            Descripcion = Optional(request.Descripcion)
        };
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
