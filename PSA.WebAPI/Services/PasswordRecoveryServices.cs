using PSA.AppCore.Services.Security;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;

namespace PSA.WebAPI.Services;

public class PasswordRecoveryPolicy : IPasswordRecoveryPolicy
{
    private readonly IConfiguration _configuration;

    public PasswordRecoveryPolicy(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TimeSpan TokenLifetime
    {
        get
        {
            var configuredMinutes = _configuration.GetValue<int?>("PasswordRecovery:TokenLifetimeMinutes") ?? 1;
            var minutes = configuredMinutes <= 0 ? 1 : configuredMinutes;
            return TimeSpan.FromMinutes(minutes);
        }
    }
}

public class PasswordRecoveryEmailSender : IPasswordRecoveryEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordRecoveryEmailSender> _logger;

    public PasswordRecoveryEmailSender(IConfiguration configuration, ILogger<PasswordRecoveryEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendRecoveryEmailAsync(string destino, string nombreUsuario, string token, DateTime fechaExpiracion)
    {
        var smtp = new SmtpSettingsDTO
        {
            Host = _configuration["SmtpSettings:Host"] ?? string.Empty,
            Port = int.TryParse(_configuration["SmtpSettings:Port"], out var port) ? port : 587,
            EnableSsl = bool.TryParse(_configuration["SmtpSettings:EnableSsl"], out var ssl) ? ssl : true,
            FromName = _configuration["SmtpSettings:FromName"] ?? string.Empty,
            FromEmail = _configuration["SmtpSettings:FromEmail"] ?? string.Empty,
            Username = _configuration["SmtpSettings:Username"] ?? string.Empty,
            Password = _configuration["SmtpSettings:Password"] ?? string.Empty
        };

        var smtpConfigurado = !string.IsNullOrWhiteSpace(smtp.Host)
            && !string.IsNullOrWhiteSpace(smtp.FromEmail)
            && !string.IsNullOrWhiteSpace(smtp.Username)
            && !string.IsNullOrWhiteSpace(smtp.Password);

        if (!smtpConfigurado)
        {
            _logger.LogError("SMTP no configurado para recuperación de contraseña. Host/FromEmail/Username/Password son obligatorios.");
            throw new InvalidOperationException("no se pudo enviar el correo de recuperación");
        }

        try
        {
            var correoService = new CorreoService(smtp);
            await correoService.EnviarCorreoRecuperacionAsync(destino, nombreUsuario, token, fechaExpiracion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo de recuperación a {Destino}", destino);
            throw new InvalidOperationException("no se pudo enviar el correo de recuperación");
        }
    }
}
