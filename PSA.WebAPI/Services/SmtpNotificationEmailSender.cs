using PSA.AppCore.Services.Notifications;

namespace PSA.WebAPI.Services;

public class SmtpNotificationEmailSender : INotificationEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpNotificationEmailSender> _logger;

    public SmtpNotificationEmailSender(IConfiguration configuration, ILogger<SmtpNotificationEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendHtmlAsync(string destino, string asunto, string cuerpoHtml)
    {
        var smtp = SmtpSettingsResolver.Resolve(_configuration);
        var missingKeys = SmtpSettingsResolver.GetMissingRequiredKeys(smtp);
        if (missingKeys.Count > 0)
        {
            _logger.LogWarning("Notificación por correo omitida por configuración SMTP incompleta. Variables faltantes: {MissingKeys}", string.Join(", ", missingKeys));
            return Task.CompletedTask;
        }

        var correoService = new CorreoService(smtp);
        correoService.EnviarCorreoHtml(destino, asunto, cuerpoHtml);
        return Task.CompletedTask;
    }
}
