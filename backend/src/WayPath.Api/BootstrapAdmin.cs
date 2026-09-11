using System.ComponentModel.DataAnnotations;
using WayPath.Application;

namespace WayPath.Api;

public static class BootstrapAdmin
{
    public static async Task Run(IServiceProvider services)
    {
        if (Console.IsInputRedirected)
            throw new InvalidOperationException("Ejecute --create-admin en una terminal interactiva.");
        Console.WriteLine("Creacion inicial de administrador. Solo funciona si aun no existe uno.");
        Console.Write("Correo: ");
        var email = Console.ReadLine() ?? "";
        Console.Write("Nombre: ");
        var name = Console.ReadLine() ?? "";
        Console.Write("Clave (12 a 128 caracteres): ");
        var password = ReadPassword();
        Console.Write("Repetir clave: ");
        if (password != ReadPassword()) throw new InvalidOperationException("Las claves no coinciden.");
        var request = new RegisterRequest(email, password, name);
        Validator.ValidateObject(request, new ValidationContext(request), true);
        // Record constructor annotations are also explicitly checked outside MVC.
        if (!new EmailAddressAttribute().IsValid(email) || email.Length > 255 ||
            name.Trim().Length is < 1 or > 100 || password.Length is < 12 or > 128)
            throw new InvalidOperationException("Revise correo, nombre y longitud de clave.");
        using var scope = services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
        var id = await service.Register(request, CancellationToken.None, firstAdministrator: true);
        Console.WriteLine($"Administrador creado: {id}. Inicie sesion desde la web.");
    }

    private static string ReadPassword()
    {
        var buffer = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace && buffer.Length > 0) buffer.Length--;
            else if (!char.IsControl(key.KeyChar) && buffer.Length < 129) buffer.Append(key.KeyChar);
        }
        Console.WriteLine();
        return buffer.ToString();
    }
}

