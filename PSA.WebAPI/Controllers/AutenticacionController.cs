using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutenticacionController : ControllerBase
    {
        private readonly AutenticacionManager _autenticacionManager;

        public AutenticacionController(AutenticacionManager autenticacionManager)
        {
            _autenticacionManager = autenticacionManager;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioDTO dto)
        {
            try
            {
                var idUsuario = await _autenticacionManager.RegistrarUsuarioAsync(dto);

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
    }
}
