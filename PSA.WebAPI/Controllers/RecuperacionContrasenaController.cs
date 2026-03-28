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

        public RecuperacionContrasenaController(IConfiguration configuration)
        {
            _configuration = configuration;
            _manager = new RecuperacionContrasenaManager();
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

                var webAppBaseUrl = _configuration["AppSettings:WebAppBaseUrl"] ?? "";
                var respuesta = _manager.GenerarToken(dto, webAppBaseUrl);

                if (respuesta.Exito &&
                    !string.IsNullOrWhiteSpace(respuesta.LinkRecuperacion) &&
                    !string.IsNullOrWhiteSpace(respuesta.CorreoDestino))
                {
                    var smtp = new SmtpSettingsDTO
                    {
                        Host = _configuration["SmtpSettings:Host"] ?? "",
                        Port = int.Parse(_configuration["SmtpSettings:Port"] ?? "587"),
                        EnableSsl = bool.Parse(_configuration["SmtpSettings:EnableSsl"] ?? "true"),
                        FromName = _configuration["SmtpSettings:FromName"] ?? "",
                        FromEmail = _configuration["SmtpSettings:FromEmail"] ?? "",
                        Username = _configuration["SmtpSettings:Username"] ?? "",
                        Password = _configuration["SmtpSettings:Password"] ?? ""
                    };

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
                if (dto == null ||
                    string.IsNullOrWhiteSpace(dto.Token) ||
                    string.IsNullOrWhiteSpace(dto.NuevaContrasena) ||
                    string.IsNullOrWhiteSpace(dto.ConfirmarContrasena))
                {
                    return BadRequest(new RespuestaRecuperacionDTO
                    {
                        Exito = false,
                        Mensaje = "Debe completar todos los campos."
                    });
                }

                var respuesta = _manager.RestablecerContrasena(dto);

                if (!respuesta.Exito)
                    return BadRequest(respuesta);

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