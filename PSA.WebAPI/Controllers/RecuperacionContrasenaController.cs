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
        public IActionResult SolicitarRecuperacion([FromBody] RecuperarContrasenaDTO dto)
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

                var webAppBaseUrl = _configuration["AppSettings:WebAppBaseUrl"];
                var respuesta = _manager.GenerarToken(dto, webAppBaseUrl);

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
        public IActionResult ValidarToken([FromBody] ValidarTokenDTO dto)
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

                var respuesta = _manager.ValidarToken(dto);
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
        public IActionResult Restablecer([FromBody] RestablecerContrasenaDTO dto)
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

                var respuesta = _manager.RestablecerContrasena(dto);
                if (!respuesta.Exito)
                {
                    return BadRequest(respuesta);
                }

                return Ok(respuesta);
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
