using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace PSA.WebApp.Controllers
{
    public class AutenticacionController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AutenticacionController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult IniciarSesion()
        {
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

            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:59665";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync($"{apiBaseUrl}/api/Autenticacion/iniciar-sesion", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"No fue posible iniciar sesión. {errorBody}");
                    return View(dto);
                }

                var respuesta = await response.Content.ReadFromJsonAsync<RespuestaInicioSesionDTO>();
                if (respuesta == null)
                {
                    ModelState.AddModelError(string.Empty, "No se recibió una respuesta válida del API.");
                    return View(dto);
                }

                TempData["MensajeExito"] = respuesta.Mensaje;
                return RedirectToAction(GetDashboardActionByRole(respuesta.IdRol), "Dashboard");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "No se pudo conectar con el API de autenticación.");
                return View(dto);
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

            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:59665";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync($"{apiBaseUrl}/api/Autenticacion/registrar", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    var errorMensaje = TryReadErrorMessage(errorBody);
                    ModelState.AddModelError(string.Empty, errorMensaje);
                    return View(dto);
                }

                TempData["MensajeExito"] = "Usuario registrado correctamente. Ya puede iniciar sesión.";
                return RedirectToAction(nameof(IniciarSesion));
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "No se pudo conectar con el API de autenticación.");
                return View(dto);
            }
        }

        [HttpGet]
        public IActionResult RecuperarContrasena()
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Recuperar contraseña";
            ViewBag.SubtituloPagina = "Ingrese su correo electrónico para iniciar el proceso de recuperación.";
            return View();
        }

        [HttpGet]
        public IActionResult RestablecerContrasena(string? token = null)
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TokenRecuperacion = token ?? "TOKEN-DE-EJEMPLO";
            ViewBag.TituloPagina = "Restablecer contraseña";
            ViewBag.SubtituloPagina = "Defina una nueva contraseña segura para su cuenta.";
            return View();
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
            }
            catch
            {
                // Se ignora error de parseo y se devuelve mensaje genérico.
            }

            return "No fue posible completar la operación.";
        }
    }
}
