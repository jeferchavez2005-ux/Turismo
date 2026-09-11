namespace WayPath.Application;

public sealed class CatalogoService(ICatalogoStore store)
{
    private static readonly HashSet<string> Difficulties = ["BAJO", "MEDIO", "ALTO", "EXTREMO"];

    public Task<DestinoResumenDto[]> Destinos(CatalogoFiltroRequest filtro, CancellationToken ct) =>
        store.ListDestinos(filtro, ct);
    public Task<DestinoDetalleDto?> Destino(string slugOrId, CancellationToken ct) =>
        store.GetDestino(slugOrId.Trim(), ct);
    public Task<PaqueteResumenDto[]> Paquetes(long? destinoId, CancellationToken ct) =>
        store.ListPaquetes(destinoId, ct);
    public Task<PaqueteDetalleDto?> Paquete(string slugOrId, CancellationToken ct) =>
        store.GetPaquete(slugOrId.Trim(), ct);
    public Task<CatalogoOpcionesDto> Options(CancellationToken ct) => store.Options(ct);

    public Task<DestinoDetalleDto> CreateDestino(DestinoRequest request, CancellationToken ct) =>
        store.CreateDestino(Clean(request), ct);
    public Task<DestinoDetalleDto> UpdateDestino(long id, DestinoRequest request, CancellationToken ct) =>
        store.UpdateDestino(id, Clean(request), ct);
    public Task SetDestinoState(long id, string codigo, CancellationToken ct) =>
        store.SetDestinoState(id, codigo.Trim().ToUpperInvariant(), ct);
    public Task<PaqueteDetalleDto> CreatePaquete(PaqueteRequest request, CancellationToken ct) =>
        store.CreatePaquete(Clean(request), ct);
    public Task<PaqueteDetalleDto> UpdatePaquete(long id, PaqueteRequest request, CancellationToken ct) =>
        store.UpdatePaquete(id, Clean(request), ct);
    public Task SetPaqueteState(long id, string codigo, CancellationToken ct) =>
        store.SetPaqueteState(id, codigo.Trim().ToUpperInvariant(), ct);

    private static DestinoRequest Clean(DestinoRequest request)
    {
        var cleaned = request with
        {
            Nombre = Required(request.Nombre, "El nombre del destino es obligatorio."),
            Slug = Slug(request.Slug),
            DescripcionCorta = Required(request.DescripcionCorta, "La descripcion corta es obligatoria."),
            DescripcionLarga = Optional(request.DescripcionLarga),
            CategoriaIds = request.CategoriaIds.Distinct().ToArray()
        };
        if (cleaned.CategoriaIds.Length == 0)
            throw new SecurityFault(400, "Seleccione al menos una categoria.");
        return cleaned;
    }

    private static PaqueteRequest Clean(PaqueteRequest request)
    {
        var level = Optional(request.NivelDificultad)?.ToUpperInvariant();
        if (level is not null && !Difficulties.Contains(level))
            throw new SecurityFault(400, "El nivel de dificultad no es valido.");
        return request with
        {
            Nombre = Required(request.Nombre, "El nombre del paquete es obligatorio."),
            Slug = Slug(request.Slug),
            Descripcion = Optional(request.Descripcion),
            NivelDificultad = level
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

    private static string Slug(string value)
    {
        var slug = Required(value, "El slug es obligatorio.").ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            throw new SecurityFault(400, "El slug solo puede contener letras minusculas, numeros y guiones.");
        return slug;
    }
}
