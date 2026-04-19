using PSA.AppCore.Services.Notifications;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;

namespace PSA.WebAPI.Services;

public class SmtpNotificationEmailSender : INotificationEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpNotificationEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task SendHtmlAsync(string destino, string asunto, string cuerpoHtml)
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
            return Task.CompletedTask;
        }

        var correoService = new CorreoService(smtp);
        correoService.EnviarCorreoHtml(destino, asunto, cuerpoHtml);
        return Task.CompletedTask;
    }
}
