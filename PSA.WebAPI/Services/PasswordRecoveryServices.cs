using PSA.AppCore.Services.Security;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;
using System.Net.Mail;

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
        var smtp = SmtpSettingsResolver.Resolve(_configuration);
        var missingKeys = SmtpSettingsResolver.GetMissingRequiredKeys(smtp);
        if (missingKeys.Count > 0)
        {
            _logger.LogError("SMTP no configurado para recuperación de contraseña. Variables faltantes: {MissingKeys}", string.Join(", ", missingKeys));
            throw new InvalidOperationException("no se pudo enviar el correo de recuperación");
        }

        try
        {
            var correoService = new CorreoService(smtp);
            await correoService.EnviarCorreoRecuperacionAsync(destino, nombreUsuario, token, fechaExpiracion);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP rechazó el correo de recuperación a {Destino}. SmtpStatusCode: {SmtpStatusCode}", destino, ex.StatusCode);
            throw new InvalidOperationException("no se pudo enviar el correo de recuperación");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo de recuperación a {Destino}", destino);
            throw new InvalidOperationException("no se pudo enviar el correo de recuperación");
        }
    }
}
