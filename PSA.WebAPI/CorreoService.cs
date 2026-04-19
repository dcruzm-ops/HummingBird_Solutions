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

        public async Task EnviarCorreoRecuperacionAsync(string destino, string nombreUsuario, string token, DateTime fechaExpiracionUtc)
        {
            var asunto = "Recuperación de contraseña - PSA Costa Rica";

            var cuerpo = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Recuperación de contraseña</h2>
                    <p>Hola {nombreUsuario},</p>
                    <p>Recibimos una solicitud para restablecer tu contraseña.</p>
                    <p>Tu token de recuperación es:</p>
                    <p style='font-size:24px;font-weight:700;letter-spacing:4px;'>{token}</p>
                    <p>Este token vence el {fechaExpiracionUtc:yyyy-MM-dd HH:mm:ss} UTC (máximo 1 minuto).</p>
                    <p>Si no solicitaste este cambio, puedes ignorar este correo.</p>
                </body>
                </html>";

            await EnviarCorreoHtmlAsync(destino, asunto, cuerpo);
        }

        public void EnviarCorreoBienvenida(string destino, string nombreUsuario, string rol, string enlaceSistema)
        {
            var asunto = "Bienvenido a PSA Costa Rica";

            var cuerpo = $@"
                <html>
                <body style='font-family: Arial, sans-serif; color:#1f2937;'>
                    <div style='max-width:640px;margin:0 auto;border:1px solid #d9e2ec;border-radius:10px;overflow:hidden;'>
                        <div style='background:#0f172a;padding:18px 24px;color:#fff;font-size:20px;font-weight:700;'>PSA Costa Rica</div>
                        <div style='padding:24px;'>
                            <h2 style='margin-top:0;color:#111827;'>Registro de cuenta confirmado</h2>
                            <p>Hola {nombreUsuario},</p>
                            <p>Tu cuenta en PSA Costa Rica fue creada correctamente.</p>
                            <p><strong>Rol asignado:</strong> {rol}</p>
                            <p>Ya puedes ingresar al sistema para registrar fincas y dar seguimiento a tus procesos.</p>
                            <p>
                                <a href='{enlaceSistema}' style='background:#16a34a;color:white;padding:10px 16px;text-decoration:none;border-radius:6px;display:inline-block;'>
                                    Ingresar al sistema
                                </a>
                            </p>
                            <p style='font-size:13px;color:#4b5563;'>Si no realizaste este registro, por favor comunícate con el administrador del sistema.</p>
                        </div>
                    </div>
                </body>
                </html>";

            EnviarCorreoHtml(destino, asunto, cuerpo);
        }

        public void EnviarCorreoTextoPlano(string destino, string asunto, string cuerpo)
        {
            using var mensaje = new MailMessage();
            mensaje.From = new MailAddress(_smtp.FromEmail, _smtp.FromName);
            mensaje.To.Add(destino);
            mensaje.Subject = asunto;
            mensaje.Body = cuerpo;
            mensaje.IsBodyHtml = false;

            using var cliente = new SmtpClient(_smtp.Host, _smtp.Port);
            cliente.Credentials = new NetworkCredential(_smtp.Username, _smtp.Password);
            cliente.EnableSsl = _smtp.EnableSsl;

            cliente.Send(mensaje);
        }

        public void EnviarCorreoHtml(string destino, string asunto, string cuerpoHtml)
        {
            using var mensaje = new MailMessage();
            mensaje.From = new MailAddress(_smtp.FromEmail, _smtp.FromName);
            mensaje.To.Add(destino);
            mensaje.Subject = asunto;
            mensaje.Body = cuerpoHtml;
            mensaje.IsBodyHtml = true;

            using var cliente = new SmtpClient(_smtp.Host, _smtp.Port);
            cliente.Credentials = new NetworkCredential(_smtp.Username, _smtp.Password);
            cliente.EnableSsl = _smtp.EnableSsl;

            cliente.Send(mensaje);
        }

        public async Task EnviarCorreoHtmlAsync(string destino, string asunto, string cuerpoHtml)
        {
            using var mensaje = new MailMessage();
            mensaje.From = new MailAddress(_smtp.FromEmail, _smtp.FromName);
            mensaje.To.Add(destino);
            mensaje.Subject = asunto;
            mensaje.Body = cuerpoHtml;
            mensaje.IsBodyHtml = true;

            using var cliente = new SmtpClient(_smtp.Host, _smtp.Port);
            cliente.Credentials = new NetworkCredential(_smtp.Username, _smtp.Password);
            cliente.EnableSsl = _smtp.EnableSsl;

            await cliente.SendMailAsync(mensaje);
        }
    }
}
