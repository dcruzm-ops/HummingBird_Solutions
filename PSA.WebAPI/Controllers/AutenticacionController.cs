using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;
using PSA.EntidadesDTO.DTOs.Usuarios;
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

        [HttpPost("asignar-rol")]
        public async Task<IActionResult> AsignarRol([FromBody] AsignarRolUsuarioDTO dto)
        {
            try
            {
                await _autenticacionManager.AsignarRolAsync(dto);
                return Ok(new { Mensaje = "Rol asignado correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Mensaje = ex.Message });
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
                var nombreUsuario = string.IsNullOrWhiteSpace(dto.NombreCompleto) ? "usuario" : dto.NombreCompleto.Trim();

                var correoService = new CorreoService(smtp);
                correoService.EnviarCorreoRecuperacion(
                    dto.Email.Trim(),
                    nombreUsuario,
                    urlLogin
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
