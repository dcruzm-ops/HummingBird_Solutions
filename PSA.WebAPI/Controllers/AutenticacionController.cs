using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;
using PSA.EntidadesDTO.DTOs.Usuarios;
using PSA.WebAPI.Services;
using PSA.WebAPI.Services.Security;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutenticacionController(
        AutenticacionManager autenticacionManager,
        IConfiguration configuration,
        RolPermisoDAO rolPermisoDao,
        UsuarioDAO usuarioDao,
        IJwtTokenService jwtTokenService,
        ISecurityThrottleService securityThrottleService) : BaseApiController
    {
        private readonly AutenticacionManager _autenticacionManager = autenticacionManager;
        private readonly IConfiguration _configuration = configuration;
        private readonly RolPermisoDAO _rolPermisoDao = rolPermisoDao;
        private readonly UsuarioDAO _usuarioDao = usuarioDao;
        private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
        private readonly ISecurityThrottleService _securityThrottleService = securityThrottleService;

        [AllowAnonymous]
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioDTO dto)
        {
            try
            {
                var idUsuario = await _autenticacionManager.RegistrarUsuarioAsync(dto);
                await IntentarEnviarCorreoBienvenidaAsync(dto);

                return ApiCreated(new
                {
                    IdUsuario = idUsuario,
                    Mensaje = "Usuario registrado correctamente."
                });
            }
            catch (Exception ex)
            {
                return ApiValidationError("No fue posible registrar el usuario.", ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("iniciar-sesion")]
        public async Task<IActionResult> IniciarSesion([FromBody] InicioSesionDTO dto)
        {
            var email = dto.Email?.Trim() ?? string.Empty;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
            var compositeKey = $"{email}|{ip}";

            if (_securityThrottleService.IsBlocked("login", compositeKey, out var retryAfter))
            {
                return ApiError(StatusCodes.Status429TooManyRequests, "rate_limited", $"Demasiados intentos. Intente nuevamente en {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes))} minuto(s).");
            }

            RespuestaInicioSesionDTO respuesta;
            try
            {
                respuesta = await _autenticacionManager.IniciarSesionAsync(dto);
                _securityThrottleService.RegisterSuccess("login", compositeKey);
            }
            catch (Exception ex)
            {
                _securityThrottleService.RegisterFailure("login", compositeKey);
                return ApiValidationError("Credenciales inválidas.", ex.Message);
            }

            var permisos = new List<string>();
            try
            {
                permisos = await _rolPermisoDao.ObtenerCodigosPermisoPorRolAsync(respuesta.IdRol) ?? [];
            }
            catch
            {
                // Fallback a lista vacía para no bloquear el inicio de sesión.
            }

            respuesta.Permisos = permisos;

            string? nombreRol = null;
            try
            {
                nombreRol = await _usuarioDao.ObtenerNombreRolPorIdAsync(respuesta.IdRol);
            }
            catch
            {
                // El nombre de rol es opcional en el token.
            }

            try
            {
                respuesta.TokenAcceso = _jwtTokenService.CreateToken(
                    respuesta.IdUsuario,
                    respuesta.IdRol,
                    respuesta.Email,
                    respuesta.NombreCompleto,
                    permisos,
                    nombreRol);
            }
            catch
            {
                // No se bloquea el login web por falla de token API.
                respuesta.TokenAcceso = string.Empty;
            }

            return ApiOk(respuesta, "Inicio de sesión exitoso.");
        }

        [Authorize(Roles = "1")]
        [HttpPost("asignar-rol")]
        public async Task<IActionResult> AsignarRol([FromBody] AsignarRolUsuarioDTO dto)
        {
            try
            {
                await _autenticacionManager.AsignarRolAsync(dto);
                return ApiOk(new { Mensaje = "Rol asignado correctamente." });
            }
            catch (Exception ex)
            {
                return ApiValidationError("No fue posible asignar el rol.", ex.Message);
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
                    EnableSsl = !bool.TryParse(_configuration["SmtpSettings:EnableSsl"], out var ssl) || ssl,
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
