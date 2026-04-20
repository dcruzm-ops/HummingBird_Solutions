using Microsoft.AspNetCore.Authorization;
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

        [AllowAnonymous]
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

        [AllowAnonymous]
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

        [Authorize(Roles = "1")]
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

                var webAppBaseUrl = _configuration["AppSettings:WebAppBaseUrl"]?.TrimEnd('/');
                var baseUrl = string.IsNullOrWhiteSpace(webAppBaseUrl)
                    ? "https://localhost:59664"
                    : webAppBaseUrl;
                var urlLogin = $"{baseUrl}/Autenticacion/IniciarSesion";
                var nombreUsuario = string.IsNullOrWhiteSpace(dto.NombreCompleto) ? "usuario" : dto.NombreCompleto.Trim();
                var rol = "Dueño de finca";

                var correoService = new CorreoService(smtp);
                correoService.EnviarCorreoBienvenida(
                    dto.Email.Trim(),
                    nombreUsuario,
                    rol,
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
