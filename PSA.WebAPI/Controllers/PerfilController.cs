using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.DataAccess.DAO;
using PSA.WebAPI.Extensions;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PerfilController : ControllerBase
    {
        private readonly UsuarioDAO _usuarioDao;

        public PerfilController(UsuarioDAO usuarioDao)
        {
            _usuarioDao = usuarioDao;
        }

        [HttpGet("mi-perfil/{idUsuario:int}")]
        public async Task<IActionResult> ObtenerMiPerfil([FromRoute] int idUsuario)
        {
            idUsuario = this.GetUserId();
            if (idUsuario <= 0)
                return BadRequest(new { Mensaje = "El idUsuario debe ser mayor a 0." });

            var perfil = await _usuarioDao.ObtenerMiPerfilAsync(idUsuario);
            if (perfil == null)
                return NotFound(new { Mensaje = "No se encontró el usuario solicitado." });

            return Ok(perfil);
        }

        [HttpPut("mi-perfil/{idUsuario:int}")]
        public async Task<IActionResult> ActualizarMiPerfil([FromRoute] int idUsuario, [FromBody] ActualizarMiPerfilRequest request)
        {
            idUsuario = this.GetUserId();
            if (idUsuario <= 0)
                return BadRequest(new { Mensaje = "El idUsuario debe ser mayor a 0." });

            if (request == null || string.IsNullOrWhiteSpace(request.NombreCompleto) || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { Mensaje = "Nombre completo y correo son obligatorios." });

            var actualizado = await _usuarioDao.ActualizarMiPerfilAsync(idUsuario, request.NombreCompleto.Trim(), request.Email.Trim());
            return actualizado
                ? Ok(new { Mensaje = "Perfil actualizado correctamente." })
                : NotFound(new { Mensaje = "No se pudo actualizar el perfil." });
        }

        public class ActualizarMiPerfilRequest
        {
            public string NombreCompleto { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }
    }
}
