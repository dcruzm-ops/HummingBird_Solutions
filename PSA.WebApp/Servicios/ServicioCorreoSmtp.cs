using System.Net;
using System.Net.Mail;

namespace PSA.WebApp.Servicios
{
    public class ServicioCorreoSmtp : IServicioCorreo
    {
        private readonly IConfiguration _configuration;

        public ServicioCorreoSmtp(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarAsync(string destinatario, string asunto, string cuerpoTextoPlano)
        {
            var host = GetSetting("EmailSettings:SmtpHost", "PSA_EMAIL_SMTP_HOST");
            var puertoRaw = GetSetting("EmailSettings:SmtpPort", "PSA_EMAIL_SMTP_PORT");
            var puerto = int.TryParse(puertoRaw, out var p) ? p : 587;
            var usuario = GetSetting("EmailSettings:Username", "PSA_EMAIL_SMTP_USERNAME");
            var password = GetSetting("EmailSettings:Password", "PSA_EMAIL_SMTP_PASSWORD");

            if (string.IsNullOrWhiteSpace(password) || password.StartsWith("__USE_ENV_", StringComparison.Ordinal))
            {
                password = GetSetting("EmailSettings:ApiKey", "PSA_EMAIL_API_KEY");
            }

            var fromEmail = GetSetting("EmailSettings:FromEmail", "PSA_EMAIL_FROM");
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                var domain = GetSetting("EmailSettings:SenderDomain", "PSA_EMAIL_SENDER_DOMAIN");
                fromEmail = !string.IsNullOrWhiteSpace(domain) ? $"do-not-reply@{domain}" : usuario;
            }

            var fromName = GetSetting("EmailSettings:FromName", "PSA_EMAIL_FROM_NAME") ?? "PSA Costa Rica";
            var sslRaw = GetSetting("EmailSettings:EnableSsl", "PSA_EMAIL_ENABLE_SSL");
            var ssl = bool.TryParse(sslRaw, out var useSsl) ? useSsl : true;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("La configuración SMTP no está completa. Configure variables de entorno seguras para credenciales.");
            }

            using var mensaje = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = asunto,
                Body = cuerpoTextoPlano,
                IsBodyHtml = false
            };
            mensaje.To.Add(destinatario);

            using var smtp = new SmtpClient(host, puerto)
            {
                EnableSsl = ssl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(usuario, password)
            };

            await smtp.SendMailAsync(mensaje);
        }

        private string? GetSetting(string configKey, string envKey)
        {
            var fromEnv = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv;
            }

            var fromConfig = _configuration[configKey];
            return string.IsNullOrWhiteSpace(fromConfig) ? null : fromConfig;
        }
    }
}
