using WayPath.Domain.Seguridad;
using WayPath.Infrastructure;

namespace WayPath.Tests;

public sealed class PasswordServiceTests
{
    private readonly PasswordService passwords = new();

    [Fact]
    public void Hash_does_not_store_plain_text_password()
    {
        var user = new Usuario { Email = "persona@example.test" };
        const string plainTextPassword = "Test-only.password.42";

        var hash = passwords.Hash(user, plainTextPassword);

        Assert.NotEqual(plainTextPassword, hash);
    }

    [Fact]
    public void Verify_accepts_correct_password()
    {
        var user = UserWithPassword("Test-only.password.42");

        Assert.True(passwords.Verify(user, "Test-only.password.42"));
    }

    [Fact]
    public void Verify_rejects_incorrect_password()
    {
        var user = UserWithPassword("Test-only.password.42");

        Assert.False(passwords.Verify(user, "Different.password.73"));
    }

    [Fact]
    public void Verify_returns_false_for_malformed_hash()
    {
        var user = new Usuario
        {
            Email = "persona@example.test",
            PasswordHash = "not-a-valid-password-hash"
        };

        Assert.False(passwords.Verify(user, "Test-only.password.42"));
    }

    private Usuario UserWithPassword(string password)
    {
        var user = new Usuario { Email = "persona@example.test" };
        user.PasswordHash = passwords.Hash(user, password);
        return user;
    }
}
