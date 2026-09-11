using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WayPath.Application;
using WayPath.Domain.Seguridad;

namespace WayPath.Infrastructure;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<Usuario> hasher = new(Options.Create(
        new PasswordHasherOptions { IterationCount = 210000 }));
    private readonly Usuario dummy = new();

    public PasswordService()
    {
        dummy.PasswordHash = hasher.HashPassword(dummy, Guid.NewGuid().ToString("N"));
    }

    public string Hash(Usuario user, string password) => hasher.HashPassword(user, password);
    public bool Verify(Usuario user, string password)
    {
        try
        {
            return hasher.VerifyHashedPassword(user, user.PasswordHash, password)
                != PasswordVerificationResult.Failed;
        }
        catch (FormatException) { return false; }
    }
    public void DummyVerify(string password) => Verify(dummy, password);
}

