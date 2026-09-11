using Microsoft.EntityFrameworkCore;
using WayPath.Application;
using WayPath.Domain.Operacion;

namespace WayPath.Infrastructure;

public sealed class OperacionStore(WayPathDbContext db) : IOperacionStore
{
    private static string Id(long id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public async Task<DisponibilidadDto[]> Disponibilidad(DisponibilidadFiltroRequest filtro, CancellationToken ct)
    {
        var activeSalida = await StateId("SALIDA", "PROGRAMADA", ct);
        var rows = await BaseSalidas()
            .Where(x => x.EstadoId == activeSalida)
            .Where(x => filtro.PaqueteId == null || x.PaqueteId == filtro.PaqueteId)
            .Where(x => filtro.Desde == null || x.FechaInicio >= filtro.Desde)
            .Where(x => filtro.Hasta == null || x.FechaInicio <= filtro.Hasta)
            .OrderBy(x => x.FechaInicio)
            .ToArrayAsync(ct);
        var salidaIds = rows.Select(x => x.SalidaId).ToArray();
        var cupos = await CuposFor(salidaIds, ct);
        var blocked = await BlockedSalidaIds(salidaIds, ct);
        return rows.Select(x =>
        {
            cupos.TryGetValue(x.SalidaId, out var salidaCupos);
            return new DisponibilidadDto(Id(x.SalidaId), Id(x.PaqueteId), x.Paquete,
                x.Destino, x.FechaInicio, x.FechaFin, x.CupoTotal, x.CupoReservado,
                x.CupoTotal - x.CupoReservado, blocked.Contains(x.SalidaId), x.Estado,
                salidaCupos ?? []);
        }).ToArray();
    }

    public async Task<SalidaDto[]> ListSalidas(SalidasFiltroRequest filtro, CancellationToken ct) =>
        await BaseSalidas()
            .Where(x => filtro.PaqueteId == null || x.PaqueteId == filtro.PaqueteId)
            .Where(x => filtro.EstadoId == null || x.EstadoId == filtro.EstadoId)
            .Where(x => filtro.Desde == null || x.FechaInicio >= filtro.Desde)
            .Where(x => filtro.Hasta == null || x.FechaInicio <= filtro.Hasta)
            .OrderByDescending(x => x.FechaInicio)
            .Select(x => ToSalidaDto(x))
            .ToArrayAsync(ct);

    public async Task<SalidaDto?> GetSalida(long salidaId, CancellationToken ct)
    {
        var row = await BaseSalidas().SingleOrDefaultAsync(x => x.SalidaId == salidaId, ct);
        return row is null ? null : ToSalidaDto(row);
    }

    public async Task<SalidaDto> CreateSalida(SalidaRequest request, CancellationToken ct)
    {
        await ValidatePackage(request.PaqueteId, ct);
        var salida = new Salida
        {
            PaqueteId = request.PaqueteId,
            EstadoId = await StateId("SALIDA", "PROGRAMADA", ct),
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            CupoTotal = request.CupoTotal,
            CupoReservado = request.CupoReservado,
            PuntoEncuentro = request.PuntoEncuentro,
            CreadoEn = DateTime.UtcNow
        };
        db.Salidas.Add(salida);
        await SaveGuard(ct);
        return await GetSalida(salida.SalidaId, ct) ?? throw new SecurityFault(500, "No se pudo leer la salida creada.");
    }

    public async Task<SalidaDto> UpdateSalida(long salidaId, SalidaRequest request, CancellationToken ct)
    {
        await ValidatePackage(request.PaqueteId, ct);
        var salida = await db.Salidas.SingleOrDefaultAsync(x => x.SalidaId == salidaId, ct)
            ?? throw new SecurityFault(404, "Salida no encontrada.");
        salida.PaqueteId = request.PaqueteId;
        salida.FechaInicio = request.FechaInicio;
        salida.FechaFin = request.FechaFin;
        salida.CupoTotal = request.CupoTotal;
        salida.CupoReservado = request.CupoReservado;
        salida.PuntoEncuentro = request.PuntoEncuentro;
        await SaveGuard(ct);
        return await GetSalida(salidaId, ct) ?? throw new SecurityFault(500, "No se pudo leer la salida actualizada.");
    }

    public async Task SetSalidaState(long salidaId, string codigo, CancellationToken ct)
    {
        var salida = await db.Salidas.SingleOrDefaultAsync(x => x.SalidaId == salidaId, ct)
            ?? throw new SecurityFault(404, "Salida no encontrada.");
        salida.EstadoId = await StateId("SALIDA", codigo, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<CupoSalidaDto[]> ListCupos(long salidaId, CancellationToken ct)
    {
        await EnsureSalida(salidaId, ct);
        return await db.CuposSalida.AsNoTracking().Where(x => x.SalidaId == salidaId)
            .OrderBy(x => x.TipoCupo)
            .Select(x => new CupoSalidaDto(Id(x.CupoSalidaId), x.TipoCupo, x.CupoTotal,
                x.CupoReservado, x.CupoTotal - x.CupoReservado)).ToArrayAsync(ct);
    }

    public async Task<CupoSalidaDto> UpsertCupo(long salidaId, CupoSalidaRequest request, CancellationToken ct)
    {
        await EnsureSalida(salidaId, ct);
        var cupo = await db.CuposSalida.SingleOrDefaultAsync(x => x.SalidaId == salidaId && x.TipoCupo == request.TipoCupo, ct);
        if (cupo is null)
        {
            cupo = new CupoSalida { SalidaId = salidaId, TipoCupo = request.TipoCupo };
            db.CuposSalida.Add(cupo);
        }
        cupo.CupoTotal = request.CupoTotal;
        cupo.CupoReservado = request.CupoReservado;
        await SaveGuard(ct);
        return new(Id(cupo.CupoSalidaId), cupo.TipoCupo, cupo.CupoTotal, cupo.CupoReservado,
            cupo.CupoTotal - cupo.CupoReservado);
    }

    public async Task<ProveedorDto[]> ListProveedores(CancellationToken ct) =>
        await BaseProveedores().OrderBy(x => x.RazonSocial)
            .Select(x => new ProveedorDto(Id(x.ProveedorId), x.RazonSocial,
                x.NombreComercial, x.Email, x.Telefono, x.Ciudad, x.Estado)).ToArrayAsync(ct);

    public async Task<ProveedorDto> CreateProveedor(ProveedorRequest request, CancellationToken ct)
    {
        await ValidateProviderReferences(request, ct);
        var proveedor = new Proveedor
        {
            TipoDocumentoId = request.TipoDocumentoId,
            NumeroDocumento = request.NumeroDocumento,
            RazonSocial = request.RazonSocial,
            NombreComercial = request.NombreComercial,
            Email = request.Email,
            Telefono = request.Telefono,
            CiudadId = request.CiudadId,
            EstadoId = await StateId("PROVEEDOR", "ACTIVO", ct),
            CreadoEn = DateTime.UtcNow
        };
        db.Proveedores.Add(proveedor);
        await SaveGuard(ct);
        return await FindProveedor(proveedor.ProveedorId, ct);
    }

    public async Task<ProveedorDto> UpdateProveedor(long proveedorId, ProveedorRequest request, CancellationToken ct)
    {
        await ValidateProviderReferences(request, ct);
        var proveedor = await db.Proveedores.SingleOrDefaultAsync(x => x.ProveedorId == proveedorId, ct)
            ?? throw new SecurityFault(404, "Proveedor no encontrado.");
        proveedor.TipoDocumentoId = request.TipoDocumentoId;
        proveedor.NumeroDocumento = request.NumeroDocumento;
        proveedor.RazonSocial = request.RazonSocial;
        proveedor.NombreComercial = request.NombreComercial;
        proveedor.Email = request.Email;
        proveedor.Telefono = request.Telefono;
        proveedor.CiudadId = request.CiudadId;
        await SaveGuard(ct);
        return await FindProveedor(proveedorId, ct);
    }

    public async Task SetProveedorState(long proveedorId, string codigo, CancellationToken ct)
    {
        var proveedor = await db.Proveedores.SingleOrDefaultAsync(x => x.ProveedorId == proveedorId, ct)
            ?? throw new SecurityFault(404, "Proveedor no encontrado.");
        proveedor.EstadoId = await StateId("PROVEEDOR", codigo, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ProveedorServicioDto[]> ListProveedorServicios(long? proveedorId, CancellationToken ct)
    {
        var query = BaseServicios();
        if (proveedorId is not null) query = query.Where(x => x.ProveedorId == Id(proveedorId.Value));
        return await query.OrderBy(x => x.Proveedor).ThenBy(x => x.Nombre).ToArrayAsync(ct);
    }

    public async Task<ProveedorServicioDto> CreateProveedorServicio(ProveedorServicioRequest request, CancellationToken ct)
    {
        await ValidateServiceReferences(request, ct);
        var service = new ProveedorServicio
        {
            ProveedorId = request.ProveedorId,
            MonedaId = request.MonedaId,
            EstadoId = await StateId("SERVICIO", "ACTIVO", ct),
            TipoServicio = request.TipoServicio,
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            CostoBase = request.CostoBase
        };
        db.ProveedorServicios.Add(service);
        await SaveGuard(ct);
        return await FindServicio(service.ProveedorServicioId, ct);
    }

    public async Task<ProveedorServicioDto> UpdateProveedorServicio(long proveedorServicioId, ProveedorServicioRequest request, CancellationToken ct)
    {
        await ValidateServiceReferences(request, ct);
        var service = await db.ProveedorServicios.SingleOrDefaultAsync(x => x.ProveedorServicioId == proveedorServicioId, ct)
            ?? throw new SecurityFault(404, "Servicio de proveedor no encontrado.");
        service.ProveedorId = request.ProveedorId;
        service.MonedaId = request.MonedaId;
        service.TipoServicio = request.TipoServicio;
        service.Nombre = request.Nombre;
        service.Descripcion = request.Descripcion;
        service.CostoBase = request.CostoBase;
        await SaveGuard(ct);
        return await FindServicio(proveedorServicioId, ct);
    }

    public async Task<RecursoOperativoDto[]> Recursos(CancellationToken ct)
    {
        var servicios = await BaseServicios().Select(x => new RecursoOperativoDto("SERVICIO",
            x.ProveedorServicioId, x.Nombre, x.Proveedor, null, x.CostoBase, x.Estado)).ToArrayAsync(ct);
        var guias = await (from g in db.Guias.AsNoTracking()
            join e in db.Estados.AsNoTracking() on g.EstadoId equals e.EstadoId
            orderby g.Nombres, g.Apellidos
            select new RecursoOperativoDto("GUIA", Id(g.GuiaId), g.Nombres + " " + g.Apellidos,
                g.Idiomas, null, null, e.Codigo)).ToArrayAsync(ct);
        var transportes = await (from t in db.Transportes.AsNoTracking()
            join e in db.Estados.AsNoTracking() on t.EstadoId equals e.EstadoId
            orderby t.TipoTransporte, t.Placa
            select new RecursoOperativoDto("TRANSPORTE", Id(t.TransporteId), t.TipoTransporte,
                t.Placa, t.Capacidad, null, e.Codigo)).ToArrayAsync(ct);
        var hoteles = await (from h in db.Hoteles.AsNoTracking()
            join e in db.Estados.AsNoTracking() on h.EstadoId equals e.EstadoId
            orderby h.Nombre
            select new RecursoOperativoDto("HOTEL", Id(h.HotelId), h.Nombre,
                h.Direccion, null, null, e.Codigo)).ToArrayAsync(ct);
        return servicios.Concat(guias).Concat(transportes).Concat(hoteles).ToArray();
    }

    public async Task<AsignacionOperacionDto[]> ListAsignaciones(long salidaId, CancellationToken ct)
    {
        await EnsureSalida(salidaId, ct);
        return await AssignmentRows().Where(x => x.SalidaId == Id(salidaId)).OrderByDescending(x => x.FechaAsignacion).ToArrayAsync(ct);
    }

    public async Task<AsignacionOperacionDto> CreateAsignacion(AsignacionOperacionRequest request, CancellationToken ct)
    {
        await ValidateAssignmentReferences(request, ct);
        var entity = new AsignacionOperacion
        {
            SalidaId = request.SalidaId,
            ProveedorServicioId = request.ProveedorServicioId,
            GuiaId = request.GuiaId,
            TransporteId = request.TransporteId,
            HotelId = request.HotelId,
            HabitacionId = request.HabitacionId,
            Observaciones = request.Observaciones,
            FechaAsignacion = DateTime.UtcNow
        };
        db.AsignacionesOperacion.Add(entity);
        await SaveGuard(ct);
        return await AssignmentRows().SingleAsync(x => x.AsignacionOperacionId == Id(entity.AsignacionOperacionId), ct);
    }

    public async Task<BloqueoDisponibilidadDto[]> ListBloqueos(long? paqueteId, long? salidaId, CancellationToken ct) =>
        await db.BloqueosDisponibilidad.AsNoTracking()
            .Where(x => paqueteId == null || x.PaqueteId == paqueteId)
            .Where(x => salidaId == null || x.SalidaId == salidaId)
            .OrderByDescending(x => x.FechaInicio)
            .Select(x => new BloqueoDisponibilidadDto(Id(x.BloqueoDisponibilidadId),
                x.PaqueteId == null ? null : Id(x.PaqueteId.Value),
                x.SalidaId == null ? null : Id(x.SalidaId.Value), x.FechaInicio,
                x.FechaFin, x.Motivo, x.CreadoPorUsuarioId == null ? null : Id(x.CreadoPorUsuarioId.Value),
                x.CreadoEn)).ToArrayAsync(ct);

    public async Task<BloqueoDisponibilidadDto> CreateBloqueo(BloqueoDisponibilidadRequest request, long? actorId, CancellationToken ct)
    {
        if (request.PaqueteId is not null) await ValidatePackage(request.PaqueteId.Value, ct);
        if (request.SalidaId is not null) await EnsureSalida(request.SalidaId.Value, ct);
        var bloqueo = new BloqueoDisponibilidad
        {
            PaqueteId = request.PaqueteId,
            SalidaId = request.SalidaId,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            Motivo = request.Motivo,
            CreadoPorUsuarioId = actorId,
            CreadoEn = DateTime.UtcNow
        };
        db.BloqueosDisponibilidad.Add(bloqueo);
        await SaveGuard(ct);
        return (await ListBloqueos(null, bloqueo.SalidaId, ct)).Single(x => x.BloqueoDisponibilidadId == Id(bloqueo.BloqueoDisponibilidadId));
    }

    public async Task<ItinerarioOperativoDto[]> ListItinerario(long salidaId, CancellationToken ct)
    {
        await EnsureSalida(salidaId, ct);
        return await db.ItinerarioOperativo.AsNoTracking().Where(x => x.SalidaId == salidaId)
            .OrderBy(x => x.DiaNumero).ThenBy(x => x.HoraInicio)
            .Select(x => new ItinerarioOperativoDto(Id(x.ItinerarioOperativoId), Id(x.SalidaId),
                x.DiaNumero, x.HoraInicio, x.HoraFin, x.Actividad, x.Descripcion)).ToArrayAsync(ct);
    }

    public async Task<ItinerarioOperativoDto> CreateItinerario(ItinerarioOperativoRequest request, CancellationToken ct)
    {
        await EnsureSalida(request.SalidaId, ct);
        var entity = new ItinerarioOperativo
        {
            SalidaId = request.SalidaId,
            DiaNumero = request.DiaNumero,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin,
            Actividad = request.Actividad,
            Descripcion = request.Descripcion
        };
        db.ItinerarioOperativo.Add(entity);
        await SaveGuard(ct);
        return (await ListItinerario(request.SalidaId, ct)).Single(x => x.ItinerarioOperativoId == Id(entity.ItinerarioOperativoId));
    }

    public async Task<OperacionOpcionesDto> Options(CancellationToken ct)
    {
        var estados = await db.Estados.AsNoTracking().Where(x => x.EsActivo)
            .OrderBy(x => x.Grupo).ThenBy(x => x.Codigo)
            .Select(x => new EstadoOpcionDto(x.EstadoId, x.Grupo, x.Codigo, x.Nombre)).ToArrayAsync(ct);
        var areas = await db.Areas.AsNoTracking().Where(x => x.EsActivo)
            .OrderBy(x => x.Nombre).Select(x => new AreaDto(x.AreaId, x.Codigo, x.Nombre)).ToArrayAsync(ct);
        return new(estados.Where(x => x.Grupo == "SALIDA").ToArray(),
            estados.Where(x => x.Grupo == "PROVEEDOR").ToArray(),
            estados.Where(x => x.Grupo is "SERVICIO" or "GUIA" or "TRANSPORTE" or "HOTEL" or "HABITACION").ToArray(), areas);
    }

    private IQueryable<SalidaRow> BaseSalidas() =>
        from s in db.Salidas.AsNoTracking()
        join p in db.Paquetes.AsNoTracking() on s.PaqueteId equals p.PaqueteId
        join d in db.Destinos.AsNoTracking() on p.DestinoId equals d.DestinoId
        join e in db.Estados.AsNoTracking() on s.EstadoId equals e.EstadoId
        select new SalidaRow(s.SalidaId, s.PaqueteId, s.EstadoId, p.Nombre, d.Nombre,
            s.FechaInicio, s.FechaFin, s.CupoTotal, s.CupoReservado, s.PuntoEncuentro, e.Codigo);

    private IQueryable<ProveedorRow> BaseProveedores() =>
        from p in db.Proveedores.AsNoTracking()
        join e in db.Estados.AsNoTracking() on p.EstadoId equals e.EstadoId
        join c0 in db.Ciudades.AsNoTracking() on p.CiudadId equals c0.CiudadId into cities
        from c in cities.DefaultIfEmpty()
        select new ProveedorRow(p.ProveedorId, p.RazonSocial, p.NombreComercial,
            p.Email, p.Telefono, c == null ? null : c.Nombre, e.Codigo);

    private IQueryable<ProveedorServicioDto> BaseServicios() =>
        from s in db.ProveedorServicios.AsNoTracking()
        join p in db.Proveedores.AsNoTracking() on s.ProveedorId equals p.ProveedorId
        join m in db.Monedas.AsNoTracking() on s.MonedaId equals m.MonedaId
        join e in db.Estados.AsNoTracking() on s.EstadoId equals e.EstadoId
        select new ProveedorServicioDto(Id(s.ProveedorServicioId), Id(s.ProveedorId),
            p.NombreComercial ?? p.RazonSocial, s.TipoServicio, s.Nombre, s.Descripcion,
            s.CostoBase, m.CodigoIso, e.Codigo);

    private IQueryable<AsignacionOperacionDto> AssignmentRows() =>
        from a in db.AsignacionesOperacion.AsNoTracking()
        join ps0 in db.ProveedorServicios.AsNoTracking() on a.ProveedorServicioId equals ps0.ProveedorServicioId into servicios
        from ps in servicios.DefaultIfEmpty()
        join g0 in db.Guias.AsNoTracking() on a.GuiaId equals g0.GuiaId into guias
        from g in guias.DefaultIfEmpty()
        join t0 in db.Transportes.AsNoTracking() on a.TransporteId equals t0.TransporteId into transportes
        from t in transportes.DefaultIfEmpty()
        join h0 in db.Hoteles.AsNoTracking() on a.HotelId equals h0.HotelId into hoteles
        from h in hoteles.DefaultIfEmpty()
        join hb0 in db.Habitaciones.AsNoTracking() on a.HabitacionId equals hb0.HabitacionId into habitaciones
        from hb in habitaciones.DefaultIfEmpty()
        select new AsignacionOperacionDto(Id(a.AsignacionOperacionId), Id(a.SalidaId),
            ps == null ? null : ps.Nombre, g == null ? null : g.Nombres + " " + g.Apellidos,
            t == null ? null : t.TipoTransporte + " " + t.Placa, h == null ? null : h.Nombre,
            hb == null ? null : hb.TipoHabitacion, a.FechaAsignacion, a.Observaciones);

    private async Task<Dictionary<long, CupoSalidaDto[]>> CuposFor(long[] salidaIds, CancellationToken ct)
    {
        var rows = await db.CuposSalida.AsNoTracking().Where(x => salidaIds.Contains(x.SalidaId))
            .OrderBy(x => x.TipoCupo)
            .Select(x => new { x.SalidaId, Cupo = new CupoSalidaDto(Id(x.CupoSalidaId),
                x.TipoCupo, x.CupoTotal, x.CupoReservado, x.CupoTotal - x.CupoReservado) })
            .ToArrayAsync(ct);
        return rows.GroupBy(x => x.SalidaId).ToDictionary(x => x.Key, x => x.Select(r => r.Cupo).ToArray());
    }

    private async Task<HashSet<long>> BlockedSalidaIds(long[] salidaIds, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var directRows = await db.BloqueosDisponibilidad.AsNoTracking()
            .Where(x => x.SalidaId != null && salidaIds.Contains(x.SalidaId.Value) && x.FechaFin >= today)
            .Select(x => x.SalidaId!.Value).ToArrayAsync(ct);
        var packageRows = await (from b in db.BloqueosDisponibilidad.AsNoTracking()
            join s in db.Salidas.AsNoTracking() on b.PaqueteId equals s.PaqueteId
            where b.PaqueteId != null && salidaIds.Contains(s.SalidaId) && b.FechaFin >= today
            select s.SalidaId).ToArrayAsync(ct);
        return directRows.Concat(packageRows).ToHashSet();
    }

    private async Task<ProveedorDto> FindProveedor(long id, CancellationToken ct) =>
        await BaseProveedores().Where(x => x.ProveedorId == id)
            .Select(x => new ProveedorDto(Id(x.ProveedorId), x.RazonSocial,
                x.NombreComercial, x.Email, x.Telefono, x.Ciudad, x.Estado)).SingleAsync(ct);

    private async Task<ProveedorServicioDto> FindServicio(long id, CancellationToken ct) =>
        await BaseServicios().SingleAsync(x => x.ProveedorServicioId == Id(id), ct);

    private async Task ValidatePackage(long paqueteId, CancellationToken ct)
    {
        if (!await db.Paquetes.AnyAsync(x => x.PaqueteId == paqueteId, ct))
            throw new SecurityFault(400, "El paquete seleccionado no existe.");
    }

    private async Task EnsureSalida(long salidaId, CancellationToken ct)
    {
        if (!await db.Salidas.AnyAsync(x => x.SalidaId == salidaId, ct))
            throw new SecurityFault(404, "Salida no encontrada.");
    }

    private async Task ValidateProviderReferences(ProveedorRequest request, CancellationToken ct)
    {
        if (request.TipoDocumentoId is not null && !await db.TiposDocumento.AnyAsync(x => x.TipoDocumentoId == request.TipoDocumentoId && x.EsActivo, ct))
            throw new SecurityFault(400, "El tipo de documento seleccionado no existe o esta inactivo.");
        if (request.CiudadId is not null && !await db.Ciudades.AnyAsync(x => x.CiudadId == request.CiudadId && x.EsActivo, ct))
            throw new SecurityFault(400, "La ciudad seleccionada no existe o esta inactiva.");
    }

    private async Task ValidateServiceReferences(ProveedorServicioRequest request, CancellationToken ct)
    {
        if (!await db.Proveedores.AnyAsync(x => x.ProveedorId == request.ProveedorId, ct))
            throw new SecurityFault(400, "El proveedor seleccionado no existe.");
        if (!await db.Monedas.AnyAsync(x => x.MonedaId == request.MonedaId && x.EsActivo, ct))
            throw new SecurityFault(400, "La moneda seleccionada no existe o esta inactiva.");
    }

    private async Task ValidateAssignmentReferences(AsignacionOperacionRequest request, CancellationToken ct)
    {
        await EnsureSalida(request.SalidaId, ct);
        if (request.ProveedorServicioId is not null && !await db.ProveedorServicios.AnyAsync(x => x.ProveedorServicioId == request.ProveedorServicioId, ct))
            throw new SecurityFault(400, "El servicio de proveedor seleccionado no existe.");
        if (request.GuiaId is not null && !await db.Guias.AnyAsync(x => x.GuiaId == request.GuiaId, ct))
            throw new SecurityFault(400, "La guia seleccionada no existe.");
        if (request.TransporteId is not null && !await db.Transportes.AnyAsync(x => x.TransporteId == request.TransporteId, ct))
            throw new SecurityFault(400, "El transporte seleccionado no existe.");
        if (request.HotelId is not null && !await db.Hoteles.AnyAsync(x => x.HotelId == request.HotelId, ct))
            throw new SecurityFault(400, "El hotel seleccionado no existe.");
        if (request.HabitacionId is not null && !await db.Habitaciones.AnyAsync(x => x.HabitacionId == request.HabitacionId, ct))
            throw new SecurityFault(400, "La habitacion seleccionada no existe.");
    }

    private async Task<int> StateId(string group, string code, CancellationToken ct) =>
        await db.Estados.Where(x => x.Grupo == group && x.Codigo == code && x.EsActivo)
            .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, $"No existe el estado {group}.{code} requerido en WayPathDB.");

    private async Task SaveGuard(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new SecurityFault(409, "No se pudo guardar porque los datos no cumplen una restriccion de WayPathDB."); }
    }

    private static SalidaDto ToSalidaDto(SalidaRow x) => new(Id(x.SalidaId), Id(x.PaqueteId),
        x.Paquete, x.FechaInicio, x.FechaFin, x.CupoTotal, x.CupoReservado,
        x.CupoTotal - x.CupoReservado, x.PuntoEncuentro, x.Estado);

    private sealed record SalidaRow(long SalidaId, long PaqueteId, int EstadoId,
        string Paquete, string Destino, DateOnly FechaInicio, DateOnly FechaFin,
        int CupoTotal, int CupoReservado, string? PuntoEncuentro, string Estado);
    private sealed record ProveedorRow(long ProveedorId, string RazonSocial,
        string? NombreComercial, string? Email, string? Telefono, string? Ciudad, string Estado);
}
