using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;
using PSA.WebAPI.Services;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutenticacionController : ControllerBase
    {
        private readonly AutenticacionManager _autenticacionManager;
        private readonly IConfiguration _configuration;

        public AutenticacionController(
            AutenticacionManager autenticacionManager,
            IConfiguration configuration)
        {
            _autenticacionManager = autenticacionManager;
            _configuration = configuration;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioDTO dto)
        {
            try
            {
                var idUsuario = await _autenticacionManager.RegistrarUsuarioAsync(dto);
                await IntentarEnviarCorreoBienvenidaAsync(dto);

                return Ok(new
                {
                    IdUsuario = idUsuario,
                    Mensaje = "Usuario registrado correctamente."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Mensaje = ex.Message
                });
            }
        }

        [HttpPost("iniciar-sesion")]
        public async Task<IActionResult> IniciarSesion([FromBody] InicioSesionDTO dto)
        {
            try
            {
                var respuesta = await _autenticacionManager.IniciarSesionAsync(dto);
                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Mensaje = ex.Message
                });
            }
        }

        private Task IntentarEnviarCorreoBienvenidaAsync(RegistrarUsuarioDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email))
                {
                    return Task.CompletedTask;
                }

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

                var urlLogin = $"{Request.Scheme}://{Request.Host}/Autenticacion/IniciarSesion";
                var correoSoporte = _configuration["SmtpSettings:SupportEmail"] ?? "soporte@psacostarica.cr";
                var fechaRegistro = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                var nombreUsuario = string.IsNullOrWhiteSpace(dto.NombreCompleto) ? "usuario" : dto.NombreCompleto.Trim();

                var cuerpo = $@"Hola {nombreUsuario},

Tu registro en PSA Costa Rica se completó de manera exitosa el {fechaRegistro}.

Ya puedes ingresar al sistema mediante el siguiente enlace:
{urlLogin}

Te recomendamos conservar este correo como comprobante de tu registro.

Si no reconoces esta acción o consideras que el registro fue realizado por error, por favor contacta al equipo de soporte:
{correoSoporte}

Gracias por formar parte de PSA Costa Rica.

Saludos,
Equipo PSA Costa Rica

Este es un correo automático. Por favor, no respondas a este mensaje.";

                var correoService = new CorreoService(smtp);
                correoService.EnviarCorreoTextoPlano(
                    dto.Email.Trim(),
                    "Bienvenido(a) a PSA Costa Rica",
                    cuerpo
                );
            }
            catch
            {
                // No se bloquea el registro si falla el correo de bienvenida.
            }

            return Task.CompletedTask;
        }
    }
}
