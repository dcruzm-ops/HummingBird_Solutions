using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.EntidadesDTO.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace PSA.WebApp.Controllers
{
    public class AutenticacionController : Controller
    {
        private readonly IHttpClientFactory? _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly AutenticacionManager _autenticacionManager;

        public AutenticacionController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            AutenticacionManager autenticacionManager)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _autenticacionManager = autenticacionManager;
        }

        public AutenticacionController(
            IConfiguration configuration,
            AutenticacionManager autenticacionManager)
        {
            _configuration = configuration;
            _autenticacionManager = autenticacionManager;
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

            try
            {
                var client = _httpClientFactory?.CreateClient("AuthApi")
                    ?? throw new InvalidOperationException("IHttpClientFactory no está disponible.");
                var response = await PostToApiAsync(client, "/api/Autenticacion/iniciar-sesion", dto);

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
                var client = _httpClientFactory?.CreateClient("AuthApi")
                    ?? throw new InvalidOperationException("IHttpClientFactory no está disponible.");
                var response = await PostToApiAsync(client, "/api/Autenticacion/registrar", dto);

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
                return await RegistrarConFallbackLocalAsync(dto);
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
                    var response = await client.PostAsJsonAsync($"{baseUrl}{path}", payload);
                    return response;
                }
                catch (Exception ex)
                {
                    ultimaExcepcion = ex;
                }
            }

            throw new InvalidOperationException(
                "No fue posible conectar con el API de autenticación en ninguna URL configurada.",
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
                TempData["MensajeExito"] = $"{respuesta.Mensaje} (modo local)";
                return RedirectToAction(GetDashboardActionByRole(respuesta.IdRol), "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(nameof(IniciarSesion), dto);
            }
        }
    }
}
