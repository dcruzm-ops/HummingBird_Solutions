using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;
using PSA.WebAPI.Services.Security;

namespace PSA.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecuperacionContrasenaController(
        RecuperacionContrasenaManager manager,
        ISecurityThrottleService securityThrottleService) : BaseApiController
    {
        private readonly RecuperacionContrasenaManager _manager = manager;
        private readonly ISecurityThrottleService _securityThrottleService = securityThrottleService;

        [HttpPost("solicitar")]
        public async Task<IActionResult> SolicitarRecuperacion([FromBody] RecuperarContrasenaDTO dto)
        {
            var correo = dto?.Correo?.Trim() ?? string.Empty;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
            var key = $"{correo}|{ip}";
            if (_securityThrottleService.IsBlocked("password-recovery-request", key, out var retryAfter))
            {
                return ApiError(StatusCodes.Status429TooManyRequests, "rate_limited", $"Demasiadas solicitudes. Intente nuevamente en {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes))} minuto(s).");
            }

            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Correo))
                {
                    _securityThrottleService.RegisterFailure("password-recovery-request", key);
                    return ApiValidationError("Solicitud inválida.", "Debe enviar un correo válido.");
                }

                await _manager.SolicitarRecuperacionAsync(dto.Correo);
                _securityThrottleService.RegisterSuccess("password-recovery-request", key);
                return ApiOk(new RespuestaRecuperacionDTO
                {
                    Exito = true,
                    Mensaje = "Solicitud procesada correctamente."
                });
            }
            catch (InvalidOperationException ex)
            {
                _securityThrottleService.RegisterFailure("password-recovery-request", key);
                return ApiValidationError("No fue posible procesar la solicitud.", "Si el correo existe y está habilitado, recibirá instrucciones.", ex.Message);
            }
            catch (Exception)
            {
                _securityThrottleService.RegisterFailure("password-recovery-request", key);
                return ApiError(StatusCodes.Status500InternalServerError, "internal_error", "Error al procesar la recuperación.");
            }
        }

        [HttpPost("validar-token")]
        public async Task<IActionResult> ValidarToken([FromBody] ValidarTokenDTO dto)
        {
            var email = dto?.Email?.Trim() ?? string.Empty;
            var token = dto?.Token?.Trim() ?? string.Empty;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
            var key = $"{email}|{ip}|{token}";

            if (_securityThrottleService.IsBlocked("password-recovery-validate", key, out var retryAfter))
            {
                return ApiError(StatusCodes.Status429TooManyRequests, "rate_limited", $"Demasiados intentos de validación. Intente nuevamente en {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes))} minuto(s).");
            }

            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.Email))
                {
                    _securityThrottleService.RegisterFailure("password-recovery-validate", key);
                    return ApiValidationError("Solicitud inválida.", "Debe enviar token y correo válidos.");
                }

                var validacion = await _manager.ValidarTokenAsync(dto.Token, dto.Email);
                var mensaje = validacion.Estado switch
                {
                    PSA.AppCore.Services.Security.EstadoTokenRecuperacion.Vigente => "Token válido.",
                    PSA.AppCore.Services.Security.EstadoTokenRecuperacion.Expirado => "token expirado",
                    PSA.AppCore.Services.Security.EstadoTokenRecuperacion.Utilizado => "token ya utilizado",
                    _ => "token inválido"
                };

                if (!validacion.EsValido)
                {
                    _securityThrottleService.RegisterFailure("password-recovery-validate", key);
                    return ApiValidationError("No fue posible validar el token.", mensaje);
                }

                _securityThrottleService.RegisterSuccess("password-recovery-validate", key);
                return ApiOk(new RespuestaRecuperacionDTO
                {
                    Exito = true,
                    Mensaje = mensaje
                });
            }
            catch (Exception)
            {
                _securityThrottleService.RegisterFailure("password-recovery-validate", key);
                return ApiError(StatusCodes.Status500InternalServerError, "internal_error", "Error al validar el token.");
            }
        }

        [HttpPost("restablecer")]
        public async Task<IActionResult> Restablecer([FromBody] RestablecerContrasenaDTO dto)
        {
            var email = dto?.Email?.Trim() ?? string.Empty;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
            var key = $"{email}|{ip}";

            if (_securityThrottleService.IsBlocked("password-recovery-reset", key, out var retryAfter))
            {
                return ApiError(StatusCodes.Status429TooManyRequests, "rate_limited", $"Demasiados intentos de restablecimiento. Intente nuevamente en {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes))} minuto(s).");
            }

            try
            {
                if (dto == null
                    || string.IsNullOrWhiteSpace(dto.Email)
                    || string.IsNullOrWhiteSpace(dto.Token)
                    || string.IsNullOrWhiteSpace(dto.NuevaContrasena)
                    || string.IsNullOrWhiteSpace(dto.ConfirmarContrasena))
                {
                    _securityThrottleService.RegisterFailure("password-recovery-reset", key);
                    return ApiValidationError("Solicitud inválida.", "Debe completar todos los campos.");
                }

                if (!string.Equals(dto.NuevaContrasena, dto.ConfirmarContrasena, StringComparison.Ordinal))
                {
                    _securityThrottleService.RegisterFailure("password-recovery-reset", key);
                    return ApiValidationError("No fue posible restablecer la contraseña.", "Las contraseñas no coinciden.");
                }

                await _manager.RestablecerContrasenaAsync(dto.Token, dto.Email, dto.NuevaContrasena);
                _securityThrottleService.RegisterSuccess("password-recovery-reset", key);
                return ApiOk(new RespuestaRecuperacionDTO
                {
                    Exito = true,
                    Mensaje = "Contraseña restablecida correctamente."
                });
            }
            catch (InvalidOperationException ex)
            {
                _securityThrottleService.RegisterFailure("password-recovery-reset", key);
                return ApiValidationError("No fue posible restablecer la contraseña.", ex.Message);
            }
            catch (Exception)
            {
                _securityThrottleService.RegisterFailure("password-recovery-reset", key);
                return ApiError(StatusCodes.Status500InternalServerError, "internal_error", "Error al restablecer la contraseña.");
            }
        }
    }
}
