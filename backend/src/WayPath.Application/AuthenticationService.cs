using WayPath.Domain.Seguridad;

namespace WayPath.Application;

public sealed class AuthenticationService(ISecurityStore store, IPasswordService passwords)
{
    public async Task<string> Register(RegisterRequest request, CancellationToken ct,
        bool firstAdministrator = false)
    {
        var name = request.Nombre.Trim();
        if (name.Length == 0)
            throw new SecurityFault(400, "El nombre es obligatorio.");
        var user = new Usuario
        {
            Email = NormalizeEmail(request.Email),
            NombreUsuario = name,
            CreadoEn = DateTime.UtcNow
        };
        user.PasswordHash = passwords.Hash(user, request.Password);
        var id = await store.CreateUser(user, firstAdministrator, ct);
        return id.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<LoginSession> Login(LoginRequest request, string? ip, string? agent,
        CancellationToken ct)
    {
        var user = await store.FindByEmail(NormalizeEmail(request.Email), ct);
        if (user is null)
        {
            passwords.DummyVerify(request.Password);
            throw InvalidLogin();
        }
        var validPassword = passwords.Verify(user, request.Password);
        if (!validPassword || !await store.IsActive(user, ct))
            throw InvalidLogin();
        return await store.StartSession(user, ip, agent, ct);
    }

    public async Task ChangePassword(long id, PasswordRequest request, CancellationToken ct)
    {
        var user = await store.FindById(id, ct) ?? throw InvalidLogin();
        if (!passwords.Verify(user, request.PasswordActual))
            throw new SecurityFault(400, "La clave actual no es correcta.");
        await store.ChangePassword(user, passwords.Hash(user, request.PasswordNueva), ct);
    }

    public Task UpdateProfile(long id, ProfileRequest request, CancellationToken ct)
    {
        var name = request.Nombre.Trim();
        if (name.Length == 0) throw new SecurityFault(400, "El nombre es obligatorio.");
        return store.UpdateProfile(id, name, ct);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static SecurityFault InvalidLogin() => new(401, "Correo o clave incorrectos, o cuenta no disponible.");
}

