namespace WayPath.Application;

public sealed class ClienteService(IClienteStore store)
{
    private static readonly HashSet<string> Genders = ["MASCULINO", "FEMENINO", "OTRO", "NO_DECLARA"];

    public Task<ClientePerfilDto?> Get(long userId, CancellationToken ct) => store.GetByUser(userId, ct);
    public Task<ClientePage> List(int page, int pageSize, CancellationToken ct) => store.List(page, pageSize, ct);
    public Task SetState(long clienteId, string codigo, CancellationToken ct) => store.SetState(clienteId, codigo, ct);
    public Task<DocumentoClienteDto[]> Documents(long userId, CancellationToken ct) => store.ListDocuments(userId, ct);
    public Task<DireccionClienteDto[]> Addresses(long userId, CancellationToken ct) => store.ListAddresses(userId, ct);
    public Task<PreferenciaClienteDto?> Preferences(long userId, CancellationToken ct) => store.GetPreferences(userId, ct);
    public Task<ClienteCatalogosDto> Catalogs(CancellationToken ct) => store.Catalogs(ct);

    public Task<ClientePerfilDto> Save(long userId, ClientePerfilRequest request, CancellationToken ct)
    {
        var cleaned = request with
        {
            Nombres = CleanRequired(request.Nombres, "Los nombres son obligatorios."),
            Apellidos = CleanRequired(request.Apellidos, "Los apellidos son obligatorios."),
            Telefono = CleanOptional(request.Telefono),
            Genero = CleanOptional(request.Genero)?.ToUpperInvariant()
        };
        if (cleaned.Genero is not null && !Genders.Contains(cleaned.Genero))
            throw new SecurityFault(400, "El genero no es valido.");
        if (cleaned.FechaNacimiento is not null && cleaned.FechaNacimiento > DateOnly.FromDateTime(DateTime.UtcNow.Date))
            throw new SecurityFault(400, "La fecha de nacimiento no puede estar en el futuro.");
        return store.UpsertForUser(userId, cleaned, ct);
    }

    public Task<DocumentoClienteDto> AddDocument(long userId, DocumentoClienteRequest request, CancellationToken ct) =>
        store.AddDocument(userId, Clean(request), ct);
    public Task<DocumentoClienteDto> UpdateDocument(long userId, long documentId, DocumentoClienteRequest request, CancellationToken ct) =>
        store.UpdateDocument(userId, documentId, Clean(request), ct);
    public Task DeleteDocument(long userId, long documentId, CancellationToken ct) =>
        store.DeleteDocument(userId, documentId, ct);
    public Task<DireccionClienteDto> AddAddress(long userId, DireccionClienteRequest request, CancellationToken ct) =>
        store.AddAddress(userId, Clean(request), ct);
    public Task<DireccionClienteDto> UpdateAddress(long userId, long addressId, DireccionClienteRequest request, CancellationToken ct) =>
        store.UpdateAddress(userId, addressId, Clean(request), ct);
    public Task DeleteAddress(long userId, long addressId, CancellationToken ct) =>
        store.DeleteAddress(userId, addressId, ct);
    public Task<PreferenciaClienteDto> SavePreferences(long userId, PreferenciaClienteRequest request, CancellationToken ct) =>
        store.UpsertPreferences(userId, request with
        {
            IdiomaPreferido = CleanRequired(request.IdiomaPreferido, "El idioma es obligatorio.").ToLowerInvariant(),
            Observaciones = CleanOptional(request.Observaciones)
        }, ct);

    private static DocumentoClienteRequest Clean(DocumentoClienteRequest request)
    {
        if (request.FechaVencimiento is not null && request.FechaVencimiento < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            throw new SecurityFault(400, "El documento no puede estar vencido.");
        return request with { NumeroDocumento = CleanRequired(request.NumeroDocumento, "El numero de documento es obligatorio.").ToUpperInvariant() };
    }

    private static DireccionClienteRequest Clean(DireccionClienteRequest request) =>
        request with
        {
            LineaDireccion = CleanRequired(request.LineaDireccion, "La direccion es obligatoria."),
            CodigoPostal = CleanOptional(request.CodigoPostal)
        };

    private static string CleanRequired(string value, string error)
    {
        var cleaned = value.Trim();
        if (cleaned.Length == 0) throw new SecurityFault(400, error);
        return cleaned;
    }

    private static string? CleanOptional(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
    }
}
