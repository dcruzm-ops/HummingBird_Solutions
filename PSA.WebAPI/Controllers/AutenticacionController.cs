using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;
using PSA.EntidadesDTO.DTOs.Usuarios;
using PSA.WebAPI.Services.Security;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutenticacionController(
        AutenticacionManager autenticacionManager,
        RolPermisoDAO rolPermisoDao,
        UsuarioDAO usuarioDao,
        IJwtTokenService jwtTokenService,
        ISecurityThrottleService securityThrottleService) : BaseApiController
    {
        private readonly AutenticacionManager _autenticacionManager = autenticacionManager;
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

            var idRolAplicacion = NormalizarIdRolAplicacion(respuesta.IdRol, nombreRol, permisos);
            respuesta.IdRol = idRolAplicacion;

            try
            {
                respuesta.TokenAcceso = _jwtTokenService.CreateToken(
                    respuesta.IdUsuario,
                    idRolAplicacion,
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

        private static int NormalizarIdRolAplicacion(int idRolActual, string? nombreRol, IReadOnlyCollection<string> permisos)
        {
            if (idRolActual is 1 or 2 or 3)
            {
                return idRolActual;
            }

            var nombreNormalizado = (nombreRol ?? string.Empty).Trim().ToLowerInvariant();
            if (nombreNormalizado.Contains("admin"))
            {
                return 1;
            }

            if (nombreNormalizado.Contains("ing"))
            {
                return 3;
            }

            if (nombreNormalizado.Contains("due") || nombreNormalizado.Contains("prop"))
            {
                return 2;
            }

            if (permisos.Any(p => p.StartsWith("ADMIN_", StringComparison.OrdinalIgnoreCase)))
            {
                return 1;
            }

            if (permisos.Any(p => p.StartsWith("ING_", StringComparison.OrdinalIgnoreCase)))
            {
                return 3;
            }

            return 2;
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
    }
}
