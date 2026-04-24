using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;

namespace PSA.WebAPI.Services;

public static class SmtpSettingsResolver
{
    public static SmtpSettingsDTO Resolve(IConfiguration configuration)
    {
        var hostRaw = Normalize(configuration["SmtpSettings:Host"]);
        var portRaw = Normalize(configuration["SmtpSettings:Port"]);
        var enableSslRaw = Normalize(configuration["SmtpSettings:EnableSsl"]);
        var fromNameRaw = Normalize(configuration["SmtpSettings:FromName"]);
        var fromEmailRaw = Normalize(configuration["SmtpSettings:FromEmail"]);
        var usernameRaw = Normalize(configuration["SmtpSettings:Username"]);
        var passwordRaw = Normalize(configuration["SmtpSettings:Password"]);

        return new SmtpSettingsDTO
        {
            Host = hostRaw,
            Port = int.TryParse(portRaw, out var port) ? port : 587,
            EnableSsl = bool.TryParse(enableSslRaw, out var ssl) ? ssl : true,
            FromName = fromNameRaw,
            FromEmail = fromEmailRaw,
            Username = usernameRaw,
            Password = passwordRaw
        };
    }

    public static List<string> GetMissingRequiredKeys(SmtpSettingsDTO smtp)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(smtp.Host))
            missing.Add("SmtpSettings__Host");
        if (string.IsNullOrWhiteSpace(smtp.FromEmail))
            missing.Add("SmtpSettings__FromEmail");
        if (string.IsNullOrWhiteSpace(smtp.Username))
            missing.Add("SmtpSettings__Username");
        if (string.IsNullOrWhiteSpace(smtp.Password))
            missing.Add("SmtpSettings__Password");

        return missing;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();

        if ((trimmed.StartsWith('"') && trimmed.EndsWith('"'))
            || (trimmed.StartsWith('\'') && trimmed.EndsWith('\'')))
        {
            trimmed = trimmed[1..^1].Trim();
        }

        return trimmed;
    }
}
