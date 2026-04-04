using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace PSA.WebApp.Controllers
{
    public class AutenticacionController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AutenticacionController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet] public IActionResult IniciarSesion() => View(new InicioSesionDTO());
        [HttpGet] public IActionResult RegistroUsuario() => View(new RegistrarUsuarioDTO());
        [HttpGet] public IActionResult RecuperarContrasena() => View(new RecuperarContrasenaDTO());
        [HttpGet] public IActionResult ValidarTokenRecuperacion() => View(new ValidarTokenRecuperacionDTO());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarSesion(InicioSesionDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            try
            {
                var response = await _httpClientFactory.CreateClient("AuthApi").PostAsJsonAsync("api/Autenticacion/iniciar-sesion", dto);
                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                    return View(dto);
                }

                var respuesta = await response.Content.ReadFromJsonAsync<RespuestaInicioSesionDTO>();
                if (respuesta == null)
                {
                    ModelState.AddModelError(string.Empty, "No se recibió una respuesta válida del servidor.");
                    return View(dto);
                }

                await IniciarSesionWebAsync(respuesta);
                return RedirectToAction(GetDashboardActionByRole(respuesta.IdRol), "Dashboard");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "No se pudo conectar con el API de autenticación. Verifique que PSA.WebAPI esté ejecutándose.");
                return View(dto);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError(string.Empty, "El API tardó demasiado en responder. Intente nuevamente.");
                return View(dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistroUsuario(RegistrarUsuarioDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);
            try
            {
                var response = await _httpClientFactory.CreateClient("AuthApi").PostAsJsonAsync("api/Autenticacion/registrar", dto);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, TryReadErrorMessage(errorBody));
                    return View(dto);
                }

                TempData["MensajeExito"] = "Usuario registrado correctamente.";
                return RedirectToAction(nameof(IniciarSesion));
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "No se pudo conectar con el API. Verifique la disponibilidad de PSA.WebAPI.");
                return View(dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperarContrasena(RecuperarContrasenaDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);
            var response = await _httpClientFactory.CreateClient("AuthApi").PostAsJsonAsync("api/RecuperacionContrasena/solicitar", dto);
            TempData[response.IsSuccessStatusCode ? "MensajeExito" : "MensajeError"] = response.IsSuccessStatusCode ? "Se procesó la solicitud de recuperación." : "No fue posible procesar la solicitud.";
            return RedirectToAction(nameof(ValidarTokenRecuperacion));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarTokenRecuperacion(ValidarTokenRecuperacionDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);
            var response = await _httpClientFactory.CreateClient("AuthApi").PostAsJsonAsync("api/RecuperacionContrasena/validar-token", dto);
            if (!response.IsSuccessStatusCode) { ModelState.AddModelError(string.Empty, "Token inválido."); return View(dto); }
            return RedirectToAction(nameof(RestablecerContrasena), new { tokenRecuperacion = dto.Token });
        }

        [HttpGet]
        public IActionResult RestablecerContrasena(string? tokenRecuperacion = null)
            => View(new RestablecerContrasenaDTO { Token = tokenRecuperacion ?? string.Empty });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(RestablecerContrasenaDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);
            var response = await _httpClientFactory.CreateClient("AuthApi").PostAsJsonAsync("api/RecuperacionContrasena/restablecer", dto);
            if (!response.IsSuccessStatusCode)
            {
                TempData["MensajeError"] = "No fue posible restablecer la contraseña.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }
            TempData["MensajeExito"] = "Contraseña restablecida correctamente.";
            return RedirectToAction(nameof(IniciarSesion));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarSesion()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Producto", "Home");
        }

        private static string GetDashboardActionByRole(int idRol) => idRol switch { 1 => "Administrador", 2 => "Dueno", 3 => "Ingeniero", _ => "Dueno" };

        private async Task IniciarSesionWebAsync(RespuestaInicioSesionDTO respuesta)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, respuesta.IdUsuario.ToString()),
                new(ClaimTypes.Name, respuesta.NombreCompleto),
                new(ClaimTypes.Email, respuesta.Email),
                new(ClaimTypes.Role, respuesta.IdRol.ToString())
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        }

        private static string TryReadErrorMessage(string? errorBody)
        {
            if (string.IsNullOrWhiteSpace(errorBody)) return "No fue posible completar la operación.";
            try
            {
                using var doc = JsonDocument.Parse(errorBody);
                if (doc.RootElement.TryGetProperty("Mensaje", out var m)) return m.GetString() ?? "No fue posible completar la operación.";
            }
            catch { }
            return "No fue posible completar la operación.";
        }
    }
}
