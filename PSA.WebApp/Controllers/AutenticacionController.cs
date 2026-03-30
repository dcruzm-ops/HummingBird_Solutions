using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;
using PSA.WebApp.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;
using PSA.WebApp.Servicios;

namespace PSA.WebApp.Controllers
{
    public class AutenticacionController : Controller
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly AutenticacionManager _autenticacionManager;
        private readonly RecuperacionContrasenaManager _recuperacionContrasenaManager;
        private readonly IServicioCorreo _servicioCorreo;

        public AutenticacionController(
            IConfiguration configuration,
            AutenticacionManager autenticacionManager,
            RecuperacionContrasenaManager recuperacionContrasenaManager,
            IServicioCorreo servicioCorreo,
            IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _autenticacionManager = autenticacionManager;
            _recuperacionContrasenaManager = recuperacionContrasenaManager;
            _servicioCorreo = servicioCorreo;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        public IActionResult IniciarSesion()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var idRolClaim = User.FindFirstValue(ClaimTypes.Role);
                var idRol = int.TryParse(idRolClaim, out var rol) ? rol : 2;
                return RedirectToAction(GetDashboardActionByRole(idRol), "Dashboard");
            }

            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Iniciar sesión";
            ViewBag.SubtituloPagina = "Acceda al sistema PSA Costa Rica con sus credenciales.";
            return View(new InicioSesionDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarSesion(InicioSesionDTO dto)
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Iniciar sesión";
            ViewBag.SubtituloPagina = "Acceda al sistema PSA Costa Rica con sus credenciales.";

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            try
            {
                var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi")
                    ?? throw new InvalidOperationException("IHttpClientFactory no está disponible.");
                var response = await PostToApiAsync(client, "/api/Autenticacion/iniciar-sesion", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, TryReadErrorMessage(errorBody));
                    return View(dto);
                }

                var respuesta = await response.Content.ReadFromJsonAsync<RespuestaInicioSesionDTO>();
                if (respuesta == null)
                {
                    ModelState.AddModelError(string.Empty, "No se recibió una respuesta válida del API.");
                    return View(dto);
                }

                await IniciarSesionWebAsync(respuesta);
                TempData["MensajeExito"] = respuesta.Mensaje;
                return RedirectToAction(GetDashboardActionByRole(respuesta.IdRol), "Dashboard");
            }
            catch
            {
                return await IniciarSesionConFallbackLocalAsync(dto);
            }
        }

        [HttpGet]
        public IActionResult RegistroUsuario()
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Registro de usuario";
            ViewBag.SubtituloPagina = "Cree su cuenta para registrar fincas y dar seguimiento a sus procesos.";
            return View(new RegistrarUsuarioDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistroUsuario(RegistrarUsuarioDTO dto)
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Registro de usuario";
            ViewBag.SubtituloPagina = "Cree su cuenta para registrar fincas y dar seguimiento a sus procesos.";

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            try
            {
                var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi")
                    ?? throw new InvalidOperationException("IHttpClientFactory no está disponible.");
                var response = await PostToApiAsync(client, "/api/Autenticacion/registrar", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, TryReadErrorMessage(errorBody));
                    return View(dto);
                }

                TempData["MensajeExito"] = "Usuario registrado correctamente. Ya puede iniciar sesión.";
                return RedirectToAction(nameof(IniciarSesion));
            }
            catch
            {
                return await RegistrarConFallbackLocalAsync(dto);
            }
        }

        [HttpGet]
        public IActionResult RecuperarContrasena()
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Recuperar contraseña";
            ViewBag.SubtituloPagina = "Ingrese su correo electrónico para iniciar el proceso de recuperación.";
            return View(new RecuperarContrasenaDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperarContrasena(RecuperarContrasenaDTO dto)
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Recuperar contraseña";
            ViewBag.SubtituloPagina = "Ingrese su correo electrónico para iniciar el proceso de recuperación.";

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            try
            {
                var (token, nombreUsuario) = await _recuperacionContrasenaManager.GenerarTokenConNombreAsync(dto.Email);
                try
                {
                    var cuerpoCorreo = $@"Estimado(a) {nombreUsuario},

Hemos recibido una solicitud para restablecer la contraseña de su cuenta en el sistema PSA Costa Rica.

Para continuar con el proceso de recuperación, utilice el siguiente token de verificación:

{token}

Este token tiene una vigencia de 3 minutos a partir del momento en que fue generado. Una vez transcurrido ese tiempo, expirará automáticamente y deberá solicitar uno nuevo.

Si usted no realizó esta solicitud, puede ignorar este correo. No se realizará ningún cambio en su cuenta sin una validación correcta del token.

Importante: este es un correo automático enviado desde una cuenta Do-Not-Reply. Por favor, no responda a este mensaje.

Atentamente,
Sistema PSA Costa Rica
Cuenta automática Do-Not-Reply";

                    await _servicioCorreo.EnviarAsync(
                        dto.Email,
                        "PSA Costa Rica - Token de recuperación de contraseña",
                        cuerpoCorreo
                    );
                    TempData["MensajeExito"] = "Se envió el token de recuperación al correo indicado.";
                }
                catch
                {
                    TempData["MensajeError"] = "El token fue generado pero no se pudo enviar el correo. Revise configuración SMTP.";
                }

                return RedirectToAction(nameof(ValidarTokenRecuperacion));
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
                return RedirectToAction(nameof(RecuperarContrasena));
            }
        }

        [HttpGet]
        public IActionResult ValidarTokenRecuperacion()
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Validar token";
            ViewBag.SubtituloPagina = "Ingrese el token recibido por correo para continuar.";
            return View(new ValidarTokenRecuperacionDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarTokenRecuperacion(ValidarTokenRecuperacionDTO dto)
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Validar token";
            ViewBag.SubtituloPagina = "Ingrese el token recibido por correo para continuar.";

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            if (!await _recuperacionContrasenaManager.TokenEsValidoAsync(dto.Token))
            {
                ModelState.AddModelError(string.Empty, "El token es inválido o expiró. Solicite uno nuevo.");
                return View(dto);
            }

            return RedirectToAction(nameof(RestablecerContrasena), new { tokenRecuperacion = dto.Token });
        }

        [HttpGet]
        public async Task<IActionResult> RestablecerContrasena(string? tokenRecuperacion = null)
        {
            if (string.IsNullOrWhiteSpace(tokenRecuperacion) || !await _recuperacionContrasenaManager.TokenEsValidoAsync(tokenRecuperacion))
            {
                TempData["MensajeError"] = "El token expiró o no es válido. Solicite uno nuevo.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }

            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Restablecer contraseña";
            ViewBag.SubtituloPagina = "Defina una nueva contraseña segura para su cuenta.";

            return View(new RestablecerContrasenaDTO
            {
                Token = tokenRecuperacion
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(RestablecerContrasenaDTO dto)
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Restablecer contraseña";
            ViewBag.SubtituloPagina = "Defina una nueva contraseña segura para su cuenta.";

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            if (!await _recuperacionContrasenaManager.TokenEsValidoAsync(dto.Token))
            {
                TempData["MensajeError"] = "El token expiró o no es válido. Solicite uno nuevo.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }

            try
            {
                await _recuperacionContrasenaManager.RestablecerContrasenaAsync(dto.Token, dto.NuevaContrasena);
                TempData["MensajeExito"] = "Contraseña restablecida correctamente. Ya puede iniciar sesión.";
                return RedirectToAction(nameof(IniciarSesion));
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
                return RedirectToAction(nameof(RecuperarContrasena));
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarSesion()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["MensajeExito"] = "Sesión cerrada correctamente.";
            return RedirectToAction("Index", "Home");
        }

        private static string GetDashboardActionByRole(int idRol)
        {
            return idRol switch
            {
                1 => "Administrador",
                2 => "Dueno",
                3 => "Ingeniero",
                _ => "Dueno"
            };
        }

        private static string TryReadErrorMessage(string? errorBody)
        {
            if (string.IsNullOrWhiteSpace(errorBody))
            {
                return "No fue posible completar la operación.";
            }

            try
            {
                using var doc = JsonDocument.Parse(errorBody);

                if (doc.RootElement.TryGetProperty("mensaje", out var mensaje))
                {
                    return mensaje.GetString() ?? "No fue posible completar la operación.";
                }

                if (doc.RootElement.TryGetProperty("Mensaje", out var mensajeMayuscula))
                {
                    return mensajeMayuscula.GetString() ?? "No fue posible completar la operación.";
                }
            }
            catch
            {
                // Se ignora error de parseo y se devuelve mensaje genérico.
            }

            return "No fue posible completar la operación.";
        }

        private async Task<HttpResponseMessage> PostToApiAsync<TRequest>(
            HttpClient client,
            string path,
            TRequest payload)
        {
            Exception? ultimaExcepcion = null;

            foreach (var baseUrl in GetApiBaseUrls())
            {
                try
                {
                    return await client.PostAsJsonAsync($"{baseUrl}{path}", payload);
                }
                catch (Exception ex)
                {
                    ultimaExcepcion = ex;
                }
            }

            throw new InvalidOperationException(
                "No fue posible conectar con el API en ninguna URL configurada.",
                ultimaExcepcion
            );
        }

        private IEnumerable<string> GetApiBaseUrls()
        {
            var configurada = _configuration["ApiSettings:BaseUrl"];

            if (!string.IsNullOrWhiteSpace(configurada))
            {
                yield return configurada.TrimEnd('/');
            }

            yield return "https://localhost:59665";
            yield return "http://localhost:59667";
        }

        private async Task<IActionResult> RegistrarConFallbackLocalAsync(RegistrarUsuarioDTO dto)
        {
            try
            {
                await _autenticacionManager.RegistrarUsuarioAsync(dto);
                TempData["MensajeExito"] = "Usuario registrado correctamente (modo local). Ya puede iniciar sesión.";
                return RedirectToAction(nameof(IniciarSesion));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(nameof(RegistroUsuario), dto);
            }
        }

        private async Task<IActionResult> IniciarSesionConFallbackLocalAsync(InicioSesionDTO dto)
        {
            try
            {
                var respuesta = await _autenticacionManager.IniciarSesionAsync(dto);
                await IniciarSesionWebAsync(respuesta);
                TempData["MensajeExito"] = $"{respuesta.Mensaje} (modo local)";
                return RedirectToAction(GetDashboardActionByRole(respuesta.IdRol), "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(nameof(IniciarSesion), dto);
            }
        }

        private async Task IniciarSesionWebAsync(RespuestaInicioSesionDTO respuesta)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, respuesta.IdUsuario.ToString()),
                new(ClaimTypes.Name, respuesta.NombreCompleto),
                new(ClaimTypes.Email, respuesta.Email),
                new(ClaimTypes.Role, respuesta.IdRol.ToString())
            };

            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identidad);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
        }

        private IActionResult RecuperarContrasenaConFallbackLocal(RecuperarContrasenaViewModel model)
        {
            try
            {
                var payload = new RecuperarContrasenaDTO { Correo = model.Correo.Trim() };
                var baseUrlWebApp = $"{Request.Scheme}://{Request.Host}";
                var respuesta = _recuperacionContrasenaManager.GenerarToken(payload, baseUrlWebApp);

                TempData["MensajeExito"] = $"{respuesta.Mensaje} (modo local, sin envío SMTP automático)";
                return RedirectToAction(nameof(IniciarSesion));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"No fue posible procesar la recuperación: {ex.Message}");
                return View(nameof(RecuperarContrasena), model);
            }
        }

        private IActionResult RestablecerContrasenaConFallbackLocal(RestablecerContrasenaViewModel model)
        {
            try
            {
                var payload = new RestablecerContrasenaDTO
                {
                    Token = model.Token.Trim(),
                    NuevaContrasena = model.NuevaContrasena,
                    ConfirmarContrasena = model.ConfirmarContrasena
                };

                var respuesta = _recuperacionContrasenaManager.RestablecerContrasena(payload);
                if (!respuesta.Exito)
                {
                    ModelState.AddModelError(string.Empty, respuesta.Mensaje);
                    return View(nameof(RestablecerContrasena), model);
                }

                TempData["MensajeExito"] = $"{respuesta.Mensaje} (modo local)";
                return RedirectToAction(nameof(IniciarSesion));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"No fue posible restablecer la contraseña: {ex.Message}");
                return View(nameof(RestablecerContrasena), model);
            }
        }
    }
}
