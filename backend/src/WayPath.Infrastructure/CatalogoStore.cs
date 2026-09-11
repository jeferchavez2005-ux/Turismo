using System.Data;
using Microsoft.EntityFrameworkCore;
using WayPath.Application;
using WayPath.Domain.Catalogo;

namespace WayPath.Infrastructure;

public sealed class CatalogoStore(WayPathDbContext db) : ICatalogoStore
{
    private static string Id(long id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public async Task<DestinoResumenDto[]> ListDestinos(CatalogoFiltroRequest filtro, CancellationToken ct)
    {
        var activeDestination = await StateId("DESTINO", "ACTIVO", ct);
        var activePackage = await StateId("PAQUETE", "ACTIVO", ct);
        var destinations = await DestinationRows(activeDestination, filtro, ct);
        var ids = destinations.Select(x => x.DestinoId).ToArray();
        var categories = await CategoriesForDestinations(ids, ct);
        var prices = await PricesForDestinations(ids, activePackage, ct);
        var images = await ImagesForDestinations(ids, ct);

        var result = destinations.Select(d =>
        {
            prices.TryGetValue(d.DestinoId, out var price);
            categories.TryGetValue(d.DestinoId, out var cats);
            images.TryGetValue(d.DestinoId, out var image);
            return new DestinoResumenDto(Id(d.DestinoId), d.Nombre, d.Slug, d.DescripcionCorta,
                image?.Url, image?.Alt, d.Ciudad, d.Region, d.Pais, d.EsDestacado,
                price?.Price, price?.Currency, cats ?? []);
        }).Where(x =>
            filtro.PrecioMinimo is null || x.PrecioDesde is null || x.PrecioDesde >= filtro.PrecioMinimo)
          .Where(x =>
            filtro.PrecioMaximo is null || x.PrecioDesde is null || x.PrecioDesde <= filtro.PrecioMaximo)
          .ToArray();
        return result;
    }

    public async Task<DestinoDetalleDto?> GetDestino(string slugOrId, CancellationToken ct)
    {
        var activeDestination = await StateId("DESTINO", "ACTIVO", ct);
        var rows = await BaseDestinationQuery().ToArrayAsync(ct);
        var row = long.TryParse(slugOrId, out var id)
            ? rows.SingleOrDefault(x => x.EstadoId == activeDestination && x.DestinoId == id)
            : rows.SingleOrDefault(x => x.EstadoId == activeDestination &&
                string.Equals(x.Slug, slugOrId, StringComparison.OrdinalIgnoreCase));
        if (row is null) return null;

        var categories = await CategoriesForDestinations([row.DestinoId], ct);
        var images = await ImagesForDestinations([row.DestinoId], ct);
        var attractions = await db.AtraccionesDestino.AsNoTracking().Where(x => x.DestinoId == row.DestinoId)
            .OrderBy(x => x.Orden).ThenBy(x => x.AtraccionDestinoId)
            .Select(x => new AtraccionDto(Id(x.AtraccionDestinoId), x.Nombre, x.Descripcion, x.DuracionEstimadaMinutos))
            .ToArrayAsync(ct);
        var packages = await ListPaquetes(row.DestinoId, ct);
        categories.TryGetValue(row.DestinoId, out var cats);
        images.TryGetValue(row.DestinoId, out var image);
        return new(row.DestinoId.ToString(), row.Nombre, row.Slug, row.DescripcionCorta,
            row.DescripcionLarga, image?.Url, image?.Alt, row.Ciudad, row.Region, row.Pais,
            row.AltitudMetros, row.EsDestacado, cats ?? [], attractions, packages);
    }

    public async Task<DestinoDetalleDto> CreateDestino(DestinoRequest request, CancellationToken ct)
    {
        await ValidateDestinationReferences(request, ct);
        var destination = new Destino
        {
            CiudadId = request.CiudadId,
            UbicacionTuristicaId = request.UbicacionTuristicaId,
            EstadoId = await StateId("DESTINO", "ACTIVO", ct),
            Nombre = request.Nombre,
            Slug = request.Slug,
            DescripcionCorta = request.DescripcionCorta,
            DescripcionLarga = request.DescripcionLarga,
            AltitudMetros = request.AltitudMetros,
            EsDestacado = request.EsDestacado,
            CreadoEn = DateTime.UtcNow
        };
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        db.Destinos.Add(destination);
        await SaveWithDuplicateGuard(ct);
        db.DestinoCategorias.AddRange(request.CategoriaIds.Select(id => new DestinoCategoria
        {
            DestinoId = destination.DestinoId,
            CategoriaDestinoId = id
        }));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetDestino(destination.DestinoId.ToString(), ct) ?? throw new SecurityFault(500, "No se pudo leer el destino creado.");
    }

    public async Task<DestinoDetalleDto> UpdateDestino(long destinoId, DestinoRequest request, CancellationToken ct)
    {
        await ValidateDestinationReferences(request, ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var destination = await db.Destinos.SingleOrDefaultAsync(x => x.DestinoId == destinoId, ct)
            ?? throw new SecurityFault(404, "Destino no encontrado.");
        destination.CiudadId = request.CiudadId;
        destination.UbicacionTuristicaId = request.UbicacionTuristicaId;
        destination.Nombre = request.Nombre;
        destination.Slug = request.Slug;
        destination.DescripcionCorta = request.DescripcionCorta;
        destination.DescripcionLarga = request.DescripcionLarga;
        destination.AltitudMetros = request.AltitudMetros;
        destination.EsDestacado = request.EsDestacado;
        destination.ActualizadoEn = DateTime.UtcNow;
        var current = await db.DestinoCategorias.Where(x => x.DestinoId == destinoId).ToArrayAsync(ct);
        db.DestinoCategorias.RemoveRange(current.Where(x => !request.CategoriaIds.Contains(x.CategoriaDestinoId)));
        db.DestinoCategorias.AddRange(request.CategoriaIds.Except(current.Select(x => x.CategoriaDestinoId))
            .Select(id => new DestinoCategoria { DestinoId = destinoId, CategoriaDestinoId = id }));
        await SaveWithDuplicateGuard(ct);
        await tx.CommitAsync(ct);
        return await GetDestino(destination.DestinoId.ToString(), ct) ?? throw new SecurityFault(500, "No se pudo leer el destino actualizado.");
    }

    public async Task SetDestinoState(long destinoId, string codigo, CancellationToken ct)
    {
        var destination = await db.Destinos.SingleOrDefaultAsync(x => x.DestinoId == destinoId, ct)
            ?? throw new SecurityFault(404, "Destino no encontrado.");
        destination.EstadoId = await StateId("DESTINO", codigo, ct);
        destination.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<PaqueteResumenDto[]> ListPaquetes(long? destinoId, CancellationToken ct)
    {
        var activePackage = await StateId("PAQUETE", "ACTIVO", ct);
        var packages = await db.Paquetes.AsNoTracking()
            .Where(x => x.EstadoId == activePackage && (destinoId == null || x.DestinoId == destinoId))
            .OrderByDescending(x => x.EsDestacado).ThenBy(x => x.Nombre).ToArrayAsync(ct);
        var prices = await PricesForPackages(packages.Select(x => x.PaqueteId).ToArray(), ct);
        return packages.Select(p =>
        {
            prices.TryGetValue(p.PaqueteId, out var price);
            return new PaqueteResumenDto(Id(p.PaqueteId), Id(p.DestinoId), p.Nombre, p.Slug,
                p.DuracionDias, p.DuracionNoches, p.NivelDificultad, p.EsDestacado,
                price?.Price, price?.Currency);
        }).ToArray();
    }

    public async Task<PaqueteDetalleDto?> GetPaquete(string slugOrId, CancellationToken ct)
    {
        var activePackage = await StateId("PAQUETE", "ACTIVO", ct);
        var packageQuery = db.Paquetes.AsNoTracking().Where(x => x.EstadoId == activePackage);
        packageQuery = long.TryParse(slugOrId, out var id) ? packageQuery.Where(x => x.PaqueteId == id) : packageQuery.Where(x => x.Slug == slugOrId);
        var package = await packageQuery.SingleOrDefaultAsync(ct);
        if (package is null) return null;
        return await PackageDetail(package, ct);
    }

    public async Task<PaqueteDetalleDto> CreatePaquete(PaqueteRequest request, CancellationToken ct)
    {
        await ValidatePackageReferences(request.DestinoId, ct);
        var package = new Paquete
        {
            DestinoId = request.DestinoId,
            EstadoId = await StateId("PAQUETE", "ACTIVO", ct),
            Nombre = request.Nombre,
            Slug = request.Slug,
            Descripcion = request.Descripcion,
            DuracionDias = request.DuracionDias,
            DuracionNoches = request.DuracionNoches,
            NivelDificultad = request.NivelDificultad,
            EsDestacado = request.EsDestacado,
            CreadoEn = DateTime.UtcNow
        };
        db.Paquetes.Add(package);
        await SaveWithDuplicateGuard(ct);
        return await PackageDetail(package, ct);
    }

    public async Task<PaqueteDetalleDto> UpdatePaquete(long paqueteId, PaqueteRequest request, CancellationToken ct)
    {
        await ValidatePackageReferences(request.DestinoId, ct);
        var package = await db.Paquetes.SingleOrDefaultAsync(x => x.PaqueteId == paqueteId, ct)
            ?? throw new SecurityFault(404, "Paquete no encontrado.");
        package.DestinoId = request.DestinoId;
        package.Nombre = request.Nombre;
        package.Slug = request.Slug;
        package.Descripcion = request.Descripcion;
        package.DuracionDias = request.DuracionDias;
        package.DuracionNoches = request.DuracionNoches;
        package.NivelDificultad = request.NivelDificultad;
        package.EsDestacado = request.EsDestacado;
        package.ActualizadoEn = DateTime.UtcNow;
        await SaveWithDuplicateGuard(ct);
        return await PackageDetail(package, ct);
    }

    public async Task SetPaqueteState(long paqueteId, string codigo, CancellationToken ct)
    {
        var package = await db.Paquetes.SingleOrDefaultAsync(x => x.PaqueteId == paqueteId, ct)
            ?? throw new SecurityFault(404, "Paquete no encontrado.");
        package.EstadoId = await StateId("PAQUETE", codigo, ct);
        package.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<CatalogoOpcionesDto> Options(CancellationToken ct)
    {
        var categories = await db.CategoriasDestino.AsNoTracking().Where(x => x.EsActivo)
            .OrderBy(x => x.Nombre)
            .Select(x => new CategoriaDto(x.CategoriaDestinoId, x.Codigo, x.Nombre)).ToArrayAsync(ct);
        var cities = await (from c in db.Ciudades.AsNoTracking()
            join r in db.Regiones.AsNoTracking() on c.RegionId equals r.RegionId
            join p in db.Paises.AsNoTracking() on r.PaisId equals p.PaisId
            where c.EsActivo && r.EsActivo && p.EsActivo
            orderby p.Nombre, r.Nombre, c.Nombre
            select new CiudadDto(c.CiudadId, c.Nombre, r.Nombre, p.Nombre)).ToArrayAsync(ct);
        return new(categories, cities);
    }

    private async Task<PaqueteDetalleDto> PackageDetail(Paquete package, CancellationToken ct)
    {
        var destination = await db.Destinos.AsNoTracking().Where(x => x.DestinoId == package.DestinoId)
            .Select(x => x.Nombre).SingleAsync(ct);
        var rates = await (from t in db.Tarifas.AsNoTracking()
            join m in db.Monedas.AsNoTracking() on t.MonedaId equals m.MonedaId
            join e in db.Estados.AsNoTracking() on t.EstadoId equals e.EstadoId
            where t.PaqueteId == package.PaqueteId && e.Codigo == "ACTIVO"
            orderby t.PrecioBase
            select new TarifaDto(Id(t.TarifaId), t.TipoTarifa, t.PrecioBase, t.PrecioNino,
                m.CodigoIso, m.Simbolo, t.VigenciaInicio, t.VigenciaFin)).ToArrayAsync(ct);
        var incluye = await db.PaqueteIncluye.AsNoTracking().Where(x => x.PaqueteId == package.PaqueteId)
            .OrderBy(x => x.Orden).Select(x => x.Descripcion).ToArrayAsync(ct);
        var noIncluye = await db.PaqueteNoIncluye.AsNoTracking().Where(x => x.PaqueteId == package.PaqueteId)
            .OrderBy(x => x.Orden).Select(x => x.Descripcion).ToArrayAsync(ct);
        var itinerary = await db.PaqueteItinerario.AsNoTracking().Where(x => x.PaqueteId == package.PaqueteId)
            .OrderBy(x => x.DiaNumero).ThenBy(x => x.Orden)
            .Select(x => new ItinerarioDto(Id(x.PaqueteItinerarioId), x.DiaNumero, x.Titulo, x.Descripcion)).ToArrayAsync(ct);
        return new(Id(package.PaqueteId), Id(package.DestinoId), destination, package.Nombre,
            package.Slug, package.Descripcion, package.DuracionDias, package.DuracionNoches,
            package.NivelDificultad, package.EsDestacado, rates, incluye, noIncluye, itinerary);
    }

    private IQueryable<DestinationRow> BaseDestinationQuery() =>
        from d in db.Destinos.AsNoTracking()
        join c in db.Ciudades.AsNoTracking() on d.CiudadId equals c.CiudadId
        join r in db.Regiones.AsNoTracking() on c.RegionId equals r.RegionId
        join p in db.Paises.AsNoTracking() on r.PaisId equals p.PaisId
        where c.EsActivo && r.EsActivo && p.EsActivo
        select new DestinationRow(d.DestinoId, d.CiudadId, d.EstadoId, d.Nombre, d.Slug,
            d.DescripcionCorta, d.DescripcionLarga, d.AltitudMetros, d.EsDestacado,
            c.Nombre, r.Nombre, p.Nombre);

    private async Task<DestinationRow[]> DestinationRows(int activeStateId, CatalogoFiltroRequest filtro, CancellationToken ct)
    {
        var rows = (await BaseDestinationQuery().ToArrayAsync(ct)).Where(x => x.EstadoId == activeStateId);
        if (!string.IsNullOrWhiteSpace(filtro.Search))
        {
            var term = filtro.Search.Trim();
            rows = rows.Where(x => x.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.DescripcionCorta.Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.Ciudad.Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.Region.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        if (filtro.CiudadId is not null) rows = rows.Where(x => x.CiudadId == filtro.CiudadId);
        if (filtro.Destacado is not null) rows = rows.Where(x => x.EsDestacado == filtro.Destacado);
        if (filtro.CategoriaId is not null)
        {
            var destinationIds = await db.DestinoCategorias.AsNoTracking()
                .Where(x => x.CategoriaDestinoId == filtro.CategoriaId)
                .Select(x => x.DestinoId).ToArrayAsync(ct);
            rows = rows.Where(x => destinationIds.Contains(x.DestinoId));
        }
        return rows.OrderByDescending(x => x.EsDestacado).ThenBy(x => x.Nombre).ToArray();
    }

    private async Task<Dictionary<long, CategoriaDto[]>> CategoriesForDestinations(long[] destinationIds, CancellationToken ct)
    {
        var rows = await (from dc in db.DestinoCategorias.AsNoTracking()
            join c in db.CategoriasDestino.AsNoTracking() on dc.CategoriaDestinoId equals c.CategoriaDestinoId
            where destinationIds.Contains(dc.DestinoId) && c.EsActivo
            orderby c.Nombre
            select new { dc.DestinoId, Categoria = new CategoriaDto(c.CategoriaDestinoId, c.Codigo, c.Nombre) }).ToArrayAsync(ct);
        return rows.GroupBy(x => x.DestinoId).ToDictionary(x => x.Key, x => x.Select(r => r.Categoria).ToArray());
    }

    private async Task<Dictionary<long, ImageInfo>> ImagesForDestinations(long[] destinationIds, CancellationToken ct)
    {
        var rows = await (from di in db.DestinoImagenes.AsNoTracking()
            join ma in db.MediaAssets.AsNoTracking() on di.MediaAssetId equals ma.MediaAssetId
            where destinationIds.Contains(di.DestinoId) && ma.EsActivo
            orderby di.EsPrincipal descending, di.Orden, di.DestinoImagenId
            select new { di.DestinoId, ma.Url, Alt = ma.TextoAlternativo }).ToArrayAsync(ct);
        return rows.GroupBy(x => x.DestinoId).ToDictionary(x => x.Key, x =>
        {
            var image = x.First();
            return new ImageInfo(image.Url, image.Alt);
        });
    }

    private async Task<Dictionary<long, PriceInfo>> PricesForDestinations(long[] destinationIds, int activePackageStateId, CancellationToken ct)
    {
        var rows = await (from p in db.Paquetes.AsNoTracking()
            join t in db.Tarifas.AsNoTracking() on p.PaqueteId equals t.PaqueteId
            join m in db.Monedas.AsNoTracking() on t.MonedaId equals m.MonedaId
            where destinationIds.Contains(p.DestinoId) && p.EstadoId == activePackageStateId && m.EsActivo
            select new { p.DestinoId, t.PrecioBase, m.CodigoIso }).ToArrayAsync(ct);
        return rows.GroupBy(x => x.DestinoId).ToDictionary(x => x.Key, x =>
        {
            var best = x.OrderBy(r => r.PrecioBase).First();
            return new PriceInfo(best.PrecioBase, best.CodigoIso);
        });
    }

    private async Task<Dictionary<long, PriceInfo>> PricesForPackages(long[] packageIds, CancellationToken ct)
    {
        var rows = await (from t in db.Tarifas.AsNoTracking()
            join m in db.Monedas.AsNoTracking() on t.MonedaId equals m.MonedaId
            where packageIds.Contains(t.PaqueteId) && m.EsActivo
            select new { t.PaqueteId, t.PrecioBase, m.CodigoIso }).ToArrayAsync(ct);
        return rows.GroupBy(x => x.PaqueteId).ToDictionary(x => x.Key, x =>
        {
            var best = x.OrderBy(r => r.PrecioBase).First();
            return new PriceInfo(best.PrecioBase, best.CodigoIso);
        });
    }

    private async Task ValidateDestinationReferences(DestinoRequest request, CancellationToken ct)
    {
        if (!await db.Ciudades.AnyAsync(x => x.CiudadId == request.CiudadId && x.EsActivo, ct))
            throw new SecurityFault(400, "La ciudad seleccionada no existe o esta inactiva.");
        if (request.UbicacionTuristicaId is not null &&
            !await db.UbicacionesTuristicas.AnyAsync(x => x.UbicacionTuristicaId == request.UbicacionTuristicaId
                && x.CiudadId == request.CiudadId, ct))
            throw new SecurityFault(400, "La ubicacion turistica no pertenece a la ciudad seleccionada.");
        if (await db.CategoriasDestino.CountAsync(x => request.CategoriaIds.Contains(x.CategoriaDestinoId) && x.EsActivo, ct)
            != request.CategoriaIds.Length)
            throw new SecurityFault(400, "Una o mas categorias no existen o estan inactivas.");
    }

    private async Task ValidatePackageReferences(long destinationId, CancellationToken ct)
    {
        var activeDestination = await StateId("DESTINO", "ACTIVO", ct);
        if (!await db.Destinos.AnyAsync(x => x.DestinoId == destinationId && x.EstadoId == activeDestination, ct))
            throw new SecurityFault(400, "El destino seleccionado no existe o esta inactivo.");
    }

    private async Task<int> StateId(string group, string code, CancellationToken ct) =>
        await db.Estados.Where(x => x.Grupo == group && x.Codigo == code && x.EsActivo)
            .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(ct)
        ?? throw new SecurityFault(409, $"No existe el estado {group}.{code} requerido en WayPathDB.");

    private async Task SaveWithDuplicateGuard(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new SecurityFault(409, "No se pudo guardar porque el slug o los datos ya existen."); }
    }

    private sealed record PriceInfo(decimal Price, string Currency);
    private sealed record ImageInfo(string Url, string? Alt);
    private sealed record DestinationRow(long DestinoId, int CiudadId, int EstadoId, string Nombre,
        string Slug, string DescripcionCorta, string? DescripcionLarga, int? AltitudMetros,
        bool EsDestacado, string Ciudad, string Region, string Pais);
}
