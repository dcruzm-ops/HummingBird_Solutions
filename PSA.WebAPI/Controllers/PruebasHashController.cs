using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PruebasHashController : ControllerBase
    {
        private readonly AutenticacionManager _autenticacionManager;

        public PruebasHashController(AutenticacionManager autenticacionManager)
        {
            _autenticacionManager = autenticacionManager;
        }

        [HttpGet("probar")]
        public IActionResult ProbarHash()
        {
            var dto = new RegistrarUsuarioDTO
            {
                Nombre = "Dennis",
                Apellidos = "Cruz",
                CorreoElectronico = "dennis@test.com",
                Contrasena = "ClaveSegura123!",
                ConfirmacionContrasena = "ClaveSegura999!",
                IdRol = 1
            };

            var usuario = _autenticacionManager.ConstruirUsuarioParaRegistro(dto);

            var validacionCorrecta = _autenticacionManager.ValidarCredenciales(
                usuario,
                "ClaveSegura123!"
            );

            var validacionIncorrecta = _autenticacionManager.ValidarCredenciales(
                usuario,
                "OtraClave999!"
            );

            return Ok(new
            {
                Correo = usuario.CorreoElectronico,
                PasswordHash = usuario.PasswordHash,
                HashFueGenerado = !string.IsNullOrWhiteSpace(usuario.PasswordHash),
                PasswordHashEsDistintoDeLaContrasenaOriginal = usuario.PasswordHash != dto.Contrasena,
                ValidacionConClaveCorrecta = validacionCorrecta,
                ValidacionConClaveIncorrecta = validacionIncorrecta
            });
        }
    }
}