using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;
using System.Net;
using System.Net.Mail;

namespace PSA.WebAPI.Services
{
    public class CorreoService
    {
        private readonly SmtpSettingsDTO _smtp;

        public CorreoService(SmtpSettingsDTO smtp)
        {
            _smtp = smtp;
        }

        public void EnviarCorreoRecuperacion(string destino, string nombreUsuario, string enlace)
        {
            var asunto = "Recuperación de contraseña - PSA Costa Rica";

            var cuerpo = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Recuperación de contraseña</h2>
                    <p>Hola {nombreUsuario},</p>
                    <p>Recibimos una solicitud para restablecer tu contraseña.</p>
                    <p>Haz clic en el siguiente enlace para continuar:</p>
                    <p>
                        <a href='{enlace}' style='background:#2f9e44;color:white;padding:10px 16px;text-decoration:none;border-radius:6px;'>
                            Restablecer contraseña
                        </a>
                    </p>
                    <p>Si no solicitaste este cambio, puedes ignorar este correo.</p>
                    <p>Este enlace expirará pronto.</p>
                </body>
                </html>";

            using var mensaje = new MailMessage();
            mensaje.From = new MailAddress(_smtp.FromEmail, _smtp.FromName);
            mensaje.To.Add(destino);
            mensaje.Subject = asunto;
            mensaje.Body = cuerpo;
            mensaje.IsBodyHtml = true;

            using var cliente = new SmtpClient(_smtp.Host, _smtp.Port);
            cliente.Credentials = new NetworkCredential(_smtp.Username, _smtp.Password);
            cliente.EnableSsl = _smtp.EnableSsl;

            cliente.Send(mensaje);
        }
    }
}
