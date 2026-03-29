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
            var host = _configuration["EmailSettings:SmtpHost"];
            var puerto = int.TryParse(_configuration["EmailSettings:SmtpPort"], out var p) ? p : 587;
            var usuario = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                fromEmail = usuario;
            }
            var fromName = _configuration["EmailSettings:FromName"] ?? "PSA Costa Rica";
            var ssl = bool.TryParse(_configuration["EmailSettings:EnableSsl"], out var useSsl) ? useSsl : true;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("La configuración SMTP no está completa.");
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
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                smtp.Credentials = new NetworkCredential(usuario, password);
            }

            await smtp.SendMailAsync(mensaje);
        }
    }
}
