using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;
using PSA.WebAPI.Services;

namespace PSA.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecuperacionContrasenaController : ControllerBase
    {
        private readonly RecuperacionContrasenaManager _manager;
        private readonly IConfiguration _configuration;

        public RecuperacionContrasenaController(
            IConfiguration configuration,
            RecuperacionContrasenaManager manager)
        {
            _configuration = configuration;
            _manager = manager;
        }

        [HttpPost("solicitar")]
        public async Task<IActionResult> SolicitarRecuperacion([FromBody] RecuperarContrasenaDTO dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Correo))
                {
                    return BadRequest(new RespuestaRecuperacionDTO
                    {
                        Exito = false,
                        Mensaje = "Debe enviar un correo válido."
                    });
                }

                var (token, nombreUsuario) = await _manager.GenerarTokenConNombreAsync(dto.Correo);
                var webAppBaseUrl = _configuration["AppSettings:WebAppBaseUrl"]?.TrimEnd('/');
                var linkRecuperacion = string.IsNullOrWhiteSpace(webAppBaseUrl)
                    ? null
                    : $"{webAppBaseUrl}/Autenticacion/RestablecerContrasena?tokenRecuperacion={Uri.EscapeDataString(token)}";

                var respuesta = new RespuestaRecuperacionDTO
                {
                    Exito = true,
                    Mensaje = "Solicitud procesada correctamente.",
                    LinkRecuperacion = linkRecuperacion,
                    CorreoDestino = dto.Correo,
                    NombreUsuario = nombreUsuario
                };

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

                if (respuesta.Exito
                    && smtpConfigurado
                    && !string.IsNullOrWhiteSpace(respuesta.LinkRecuperacion)
                    && !string.IsNullOrWhiteSpace(respuesta.CorreoDestino))
                {
                    var correoService = new CorreoService(smtp);
                    correoService.EnviarCorreoRecuperacion(
                        respuesta.CorreoDestino,
                        respuesta.NombreUsuario ?? "usuario",
                        respuesta.LinkRecuperacion
                    );
                }
                else if (respuesta.Exito && !smtpConfigurado)
                {
                    respuesta.Mensaje = "Solicitud procesada. SMTP no configurado, por lo que no se envió correo de recuperación.";
                }

                respuesta.LinkRecuperacion = null;
                respuesta.CorreoDestino = null;
                respuesta.NombreUsuario = null;

                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RespuestaRecuperacionDTO
                {
                    Exito = false,
                    Mensaje = $"Error al procesar la recuperación: {ex.Message}"
                });
            }
        }

        [HttpPost("validar-token")]
        public async Task<IActionResult> ValidarToken([FromBody] ValidarTokenDTO dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Token))
                {
                    return BadRequest(new RespuestaRecuperacionDTO
                    {
                        Exito = false,
                        Mensaje = "Debe enviar un token válido."
                    });
                }

                var esValido = await _manager.TokenEsValidoAsync(dto.Token);
                var respuesta = new RespuestaRecuperacionDTO
                {
                    Exito = esValido,
                    Mensaje = esValido
                        ? "Token válido."
                        : "El token es inválido o expiró."
                };
                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RespuestaRecuperacionDTO
                {
                    Exito = false,
                    Mensaje = $"Error al validar el token: {ex.Message}"
                });
            }
        }

        [HttpPost("restablecer")]
        public async Task<IActionResult> Restablecer([FromBody] RestablecerContrasenaDTO dto)
        {
            try
            {
                if (dto == null
                    || string.IsNullOrWhiteSpace(dto.Token)
                    || string.IsNullOrWhiteSpace(dto.NuevaContrasena)
                    || string.IsNullOrWhiteSpace(dto.ConfirmarContrasena))
                {
                    return BadRequest(new RespuestaRecuperacionDTO
                    {
                        Exito = false,
                        Mensaje = "Debe completar todos los campos."
                    });
                }

                if (!string.Equals(dto.NuevaContrasena, dto.ConfirmarContrasena, StringComparison.Ordinal))
                {
                    return BadRequest(new RespuestaRecuperacionDTO
                    {
                        Exito = false,
                        Mensaje = "Las contraseñas no coinciden."
                    });
                }

                await _manager.RestablecerContrasenaAsync(dto.Token, dto.NuevaContrasena);
                return Ok(new RespuestaRecuperacionDTO
                {
                    Exito = true,
                    Mensaje = "Contraseña restablecida correctamente."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RespuestaRecuperacionDTO
                {
                    Exito = false,
                    Mensaje = $"Error al restablecer la contraseña: {ex.Message}"
                });
            }
        }
    }
}
