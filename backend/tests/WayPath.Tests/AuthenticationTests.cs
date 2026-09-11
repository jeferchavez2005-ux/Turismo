using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace WayPath.Tests;

public sealed class AuthenticationTests
{
    private static async Task<HttpResponseMessage> Send(HttpClient client, string path, object body,
        HttpMethod? method = null)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(method ?? HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }
    private static Task<HttpResponseMessage> Login(HttpClient client, string email = "cliente@example.test",
        string password = FakeSecurityStore.TestPassword) =>
        Send(client, "/api/auth/login", new { email, password });

    [Fact]
    public async Task Anonymous_requests_are_rejected()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/security/users")).StatusCode);
    }

    [Fact]
    public async Task Production_requires_https_and_sets_secure_cookies()
    {
        await using var app = new SecurityFactory { EnvironmentName = "Production" };
        using var insecure = app.Browser();
        Assert.Equal(HttpStatusCode.BadRequest, (await insecure.GetAsync("/health/live")).StatusCode);
        using var secure = app.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var response = await Login(secure);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("WayPath.Access="));
        Assert.Contains("secure", cookie);
    }

    [Fact]
    public async Task Csrf_token_cannot_be_reused_from_another_browser()
    {
        await using var app = new SecurityFactory();
        using var first = app.Browser();
        using var second = app.Browser();
        var csrf = await first.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = "cliente@example.test", password = FakeSecurityStore.TestPassword })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await second.SendAsync(request)).StatusCode);
        Assert.Empty(app.Store.Sessions);
    }

    [Fact]
    public async Task Login_creates_httponly_session_and_never_returns_password_or_jwt()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        var response = await Login(client);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("WayPath.Access="));
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=strict", cookie);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("1", me.GetProperty("usuarioId").GetString());
        Assert.Equal("CLIENTE", me.GetProperty("roles")[0].GetString());
    }

    [Theory]
    [InlineData("cliente@example.test", "incorrecta")]
    [InlineData("ausente@example.test", "incorrecta")]
    public async Task Invalid_credentials_have_generic_response(string email, string password)
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        var response = await Login(client, email, password);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Correo o clave incorrectos", await response.Content.ReadAsStringAsync());
        Assert.Empty(app.Store.Sessions);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Inactive_users_cannot_login(int state)
    {
        await using var app = new SecurityFactory();
        app.Store.Users[1].EstadoId = state;
        using var client = app.Browser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client)).StatusCode);
    }

    [Fact]
    public async Task Csrf_is_required_even_on_anonymous_login_and_register()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        var body = new { email = "cliente@example.test", password = FakeSecurityStore.TestPassword };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register",
            new { body.email, body.password, nombre = "Prueba" })).StatusCode);
        Assert.Empty(app.Store.Sessions);
        Assert.Equal(2, app.Store.Users.Count);
    }

    [Fact]
    public async Task Registration_hashes_password_and_assigns_only_cliente()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        var response = await Send(client, "/api/auth/register",
            new { email = "NUEVO@example.test", password = FakeSecurityStore.TestPassword, nombre = " Nuevo " });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = app.Store.Users[3];
        Assert.Equal("nuevo@example.test", user.Email);
        Assert.Equal("Nuevo", user.NombreUsuario);
        Assert.NotEqual(FakeSecurityStore.TestPassword, user.PasswordHash);
        Assert.False(user.EmailConfirmado);
        Assert.Equal(["CLIENTE"], app.Store.Roles[3]);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, user.Email)).StatusCode);
    }

    [Fact]
    public async Task Registration_rejects_privilege_injection_and_unknown_fields()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        var response = await Send(client, "/api/auth/register", new {
            email = "nuevo@example.test", password = FakeSecurityStore.TestPassword,
            nombre = "Nuevo", roles = new[] { "ADMINISTRADOR" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2, app.Store.Users.Count);
    }

    [Theory]
    [InlineData("bad-email", "Valid.password.123", "Nuevo")]
    [InlineData("nuevo@example.test", "short", "Nuevo")]
    [InlineData("nuevo@example.test", "Valid.password.123", " ")]
    public async Task Registration_validates_input(string email, string password, string nombre)
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        var response = await Send(client, "/api/auth/register", new { email, password, nombre });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2, app.Store.Users.Count);
    }

    [Fact]
    public async Task Duplicate_email_does_not_create_a_second_account()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        var response = await Send(client, "/api/auth/register", new {
            email = "CLIENTE@example.test", password = FakeSecurityStore.TestPassword, nombre = "Duplicado"
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(2, app.Store.Users.Count);
    }

    [Fact]
    public async Task Logout_revokes_a_previously_issued_token()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        var login = await Login(client);
        var cookie = login.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("WayPath.Access=")).Split(';')[0];
        Assert.Equal(HttpStatusCode.NoContent, (await Send(client, "/api/auth/logout", new { })).StatusCode);
        using var replay = app.CreateClient(new() { HandleCookies = false });
        replay.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Password_change_revokes_all_sessions()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        using var second = app.Browser();
        await Login(client);
        await Login(second);
        var response = await Send(client, "/api/auth/change-password", new {
            passwordActual = FakeSecurityStore.TestPassword, passwordNueva = "A.different.password.44"
        });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(app.Store.Sessions);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(client, password: "A.different.password.44")).StatusCode);
    }

    [Fact]
    public async Task Wrong_current_password_cannot_change_credentials()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        await Login(client);
        var hash = app.Store.Users[1].PasswordHash;
        var response = await Send(client, "/api/auth/change-password", new {
            passwordActual = "wrong", passwordNueva = "A.different.password.44"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(hash, app.Store.Users[1].PasswordHash);
    }

    [Fact]
    public async Task Current_database_roles_and_permissions_are_required()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        await Login(client, "admin@example.test");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/security/users")).StatusCode);
        app.Store.Permissions[2] = [];
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/security/users")).StatusCode);
        app.Store.Permissions[2] = ["SEGURIDAD_USUARIOS_GESTIONAR"];
        app.Store.Roles[2] = ["CLIENTE"];
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/security/users")).StatusCode);
        app.Store.Users[2].EstadoId = 2;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Users_cannot_change_other_accounts_via_profile()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        await Login(client);
        var response = await Send(client, "/api/auth/profile", new { nombre = "Cambio", usuarioId = "2" }, HttpMethod.Put);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Prueba", app.Store.Users[2].NombreUsuario);
        response = await Send(client, "/api/auth/profile", new { nombre = "Cambio" }, HttpMethod.Put);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Cambio", app.Store.Users[1].NombreUsuario);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Forged_expired_or_unregistered_tokens_are_rejected(bool forged, bool expired)
    {
        await using var app = new SecurityFactory();
        using var client = app.CreateClient(new() { HandleCookies = false });
        var key = forged ? new byte[64] : Convert.FromBase64String(SecurityFactory.SigningKey);
        var token = new JwtSecurityToken("WayPath", "WayPath.Web",
            [new Claim("sub", "1"), new Claim("jti", "unknown"), new Claim("role", "ADMINISTRADOR")],
            DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow.AddMinutes(expired ? -1 : 5),
            new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Credential_endpoint_is_rate_limited()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        for (var i = 0; i < 10; i++) await Login(client, password: "incorrecta");
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Login(client, password: "incorrecta")).StatusCode);
    }

    [Fact]
    public async Task User_list_does_not_expose_password_hashes()
    {
        await using var app = new SecurityFactory();
        using var client = app.Browser();
        await Login(client, "admin@example.test");
        var response = await client.GetStringAsync("/api/security/users");
        Assert.DoesNotContain("password", response, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(app.Store.Users[1].PasswordHash, response);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/security/users?pageSize=1000")).StatusCode);
    }
}
