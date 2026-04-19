using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.AppCore.Services.Security;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;

namespace PSA.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecuperacionContrasenaController : ControllerBase
    {
        private readonly RecuperacionContrasenaManager _manager;

        public RecuperacionContrasenaController(RecuperacionContrasenaManager manager)
        {
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

                await _manager.SolicitarRecuperacionAsync(dto.Correo);
                return Ok(new RespuestaRecuperacionDTO
                {
                    Exito = true,
                    Mensaje = "Solicitud procesada correctamente."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new RespuestaRecuperacionDTO
                {
                    Exito = false,
                    Mensaje = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new RespuestaRecuperacionDTO
                {
                    Exito = false,
                    Mensaje = "Error al procesar la recuperación."
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

                var validacion = await _manager.ValidarTokenAsync(dto.Token);
                var mensaje = validacion.Estado switch
                {
                    EstadoTokenRecuperacion.Vigente => "Token válido.",
                    EstadoTokenRecuperacion.Expirado => "token expirado",
                    EstadoTokenRecuperacion.Utilizado => "token ya utilizado",
                    _ => "token inválido"
                };

                return Ok(new RespuestaRecuperacionDTO
                {
                    Exito = validacion.EsValido,
                    Mensaje = mensaje
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new RespuestaRecuperacionDTO
                {
                    Exito = false,
                    Mensaje = "Error al validar el token."
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new RespuestaRecuperacionDTO
                {
                    Exito = false,
                    Mensaje = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new RespuestaRecuperacionDTO
                {
                    Exito = false,
                    Mensaje = "Error al restablecer la contraseña."
                });
            }
        }
    }
}
