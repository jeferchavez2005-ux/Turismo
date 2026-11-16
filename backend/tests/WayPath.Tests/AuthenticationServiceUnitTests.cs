using WayPath.Application;
using WayPath.Infrastructure;

namespace WayPath.Tests;

public sealed class AuthenticationServiceUnitTests
{
    [Fact]
    public async Task Register_normalizes_email()
    {
        var (service, store) = CreateService();

        var id = await service.Register(
            new RegisterRequest("  NEW.User@Example.Test  ", FakeSecurityStore.TestPassword, "Nuevo"),
            CancellationToken.None);

        Assert.Equal("new.user@example.test", store.Users[long.Parse(id)].Email);
    }

    [Fact]
    public async Task Register_trims_external_name_whitespace()
    {
        var (service, store) = CreateService();

        var id = await service.Register(
            new RegisterRequest("nuevo@example.test", FakeSecurityStore.TestPassword, "  Maria del Carmen  "),
            CancellationToken.None);

        Assert.Equal("Maria del Carmen", store.Users[long.Parse(id)].NombreUsuario);
    }

    [Fact]
    public async Task Register_rejects_blank_name()
    {
        var (service, store) = CreateService();
        var initialCount = store.Users.Count;

        var fault = await Assert.ThrowsAsync<SecurityFault>(() => service.Register(
            new RegisterRequest("nuevo@example.test", FakeSecurityStore.TestPassword, "   "),
            CancellationToken.None));

        Assert.Equal(400, fault.Status);
        Assert.Equal("El nombre es obligatorio.", fault.Message);
        Assert.Equal(initialCount, store.Users.Count);
    }

    [Fact]
    public async Task Login_for_unknown_user_returns_generic_response()
    {
        var (service, _) = CreateService();

        var fault = await Assert.ThrowsAsync<SecurityFault>(() => service.Login(
            new LoginRequest("unknown@example.test", "Incorrect.password.73"), null, null,
            CancellationToken.None));

        AssertGenericInvalidLogin(fault);
    }

    [Fact]
    public async Task Login_rejects_incorrect_password()
    {
        var (service, store) = CreateService();

        var fault = await Assert.ThrowsAsync<SecurityFault>(() => service.Login(
            new LoginRequest("cliente@example.test", "Incorrect.password.73"), null, null,
            CancellationToken.None));

        AssertGenericInvalidLogin(fault);
        Assert.Empty(store.Sessions);
    }

    [Fact]
    public async Task Login_rejects_inactive_user()
    {
        var (service, store) = CreateService();
        store.Users[1].EstadoId = 2;

        var fault = await Assert.ThrowsAsync<SecurityFault>(() => service.Login(
            new LoginRequest("cliente@example.test", FakeSecurityStore.TestPassword), null, null,
            CancellationToken.None));

        AssertGenericInvalidLogin(fault);
        Assert.Empty(store.Sessions);
    }

    [Fact]
    public async Task ChangePassword_with_incorrect_current_password_keeps_credentials()
    {
        var (service, store) = CreateService();
        var originalHash = store.Users[1].PasswordHash;

        var fault = await Assert.ThrowsAsync<SecurityFault>(() => service.ChangePassword(1,
            new PasswordRequest("Incorrect.password.73", "Different.password.73"),
            CancellationToken.None));

        Assert.Equal(400, fault.Status);
        Assert.Equal("La clave actual no es correcta.", fault.Message);
        Assert.Equal(originalHash, store.Users[1].PasswordHash);
    }

    [Fact]
    public async Task UpdateProfile_normalizes_name()
    {
        var (service, store) = CreateService();

        await service.UpdateProfile(1, new ProfileRequest("  Maria del Carmen  "), CancellationToken.None);

        Assert.Equal("Maria del Carmen", store.Users[1].NombreUsuario);
    }

    [Fact]
    public async Task UpdateProfile_rejects_blank_name()
    {
        var (service, store) = CreateService();
        var originalName = store.Users[1].NombreUsuario;

        var fault = await Assert.ThrowsAsync<SecurityFault>(() => service.UpdateProfile(
            1, new ProfileRequest("   "), CancellationToken.None));

        Assert.Equal(400, fault.Status);
        Assert.Equal("El nombre es obligatorio.", fault.Message);
        Assert.Equal(originalName, store.Users[1].NombreUsuario);
    }

    private static (AuthenticationService Service, FakeSecurityStore Store) CreateService()
    {
        var store = new FakeSecurityStore();
        return (new AuthenticationService(store, new PasswordService()), store);
    }

    private static void AssertGenericInvalidLogin(SecurityFault fault)
    {
        Assert.Equal(401, fault.Status);
        Assert.Equal("Correo o clave incorrectos, o cuenta no disponible.", fault.Message);
    }
}
