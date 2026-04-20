using System.Text.RegularExpressions;

namespace PSA.AppCore.Services.Security;

public interface IPasswordPolicy
{
    bool IsValid(string? password);
    string RequirementsMessage { get; }
}

public class PasswordPolicy : IPasswordPolicy
{
    private static readonly Regex PasswordRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{10,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string RequirementsMessage => "La contraseña debe tener mínimo 10 caracteres e incluir mayúscula, minúscula, número y símbolo.";

    public bool IsValid(string? password) =>
        !string.IsNullOrWhiteSpace(password) && PasswordRegex.IsMatch(password.Trim());
}
