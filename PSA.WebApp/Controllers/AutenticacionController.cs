using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.RecuperacionContrasena;
using PSA.WebApp.Models;
using System.Text;
using System.Text.Json;

namespace PSA.WebApp.Controllers
{
    public class AutenticacionController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;


        public AutenticacionController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult IniciarSesion()
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Iniciar sesión";
            ViewBag.SubtituloPagina = "Acceda al sistema PSA Costa Rica con sus credenciales.";
            return View();
        }

        [HttpGet]
        public IActionResult RegistroUsuario()
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Registro de usuario";
            ViewBag.SubtituloPagina = "Cree su cuenta para registrar fincas y dar seguimiento a sus procesos.";
            return View();
        }

        [HttpGet]
        public IActionResult RecuperarContrasena()
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Recuperar contraseña";
            ViewBag.SubtituloPagina = "Ingrese su correo electrónico para iniciar el proceso de recuperación.";
            return View(new RecuperarContrasenaViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> RecuperarContrasena(RecuperarContrasenaViewModel model)
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Recuperar contraseña";
            ViewBag.SubtituloPagina = "Ingrese su correo electrónico para iniciar el proceso de recuperación.";

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

                var dto = new RecuperarContrasenaDTO
                {
                    Correo = model.Correo
                };

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{apiBaseUrl}/api/RecuperacionContrasena/solicitar",
                    content
                );

                var responseBody = await response.Content.ReadAsStringAsync();

                var resultado = JsonSerializer.Deserialize<RespuestaRecuperacionDTO>(
                    responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = resultado?.Mensaje ?? "No se pudo procesar la solicitud.";
                    return View(model);
                }

                ViewBag.Mensaje = resultado?.Mensaje ?? "Si el correo existe en el sistema, se envió un enlace de recuperación.";

                ModelState.Clear();
                return View(new RecuperarContrasenaViewModel());
            }
            catch
            {
                ViewBag.Error = "Ocurrió un error al intentar procesar la solicitud.";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> RestablecerContrasena(string? token = null)
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Restablecer contraseña";
            ViewBag.SubtituloPagina = "Defina una nueva contraseña segura para su cuenta.";

            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Error = "El enlace es inválido.";
                return View(new RestablecerContrasenaViewModel());
            }

            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

                var dto = new ValidarTokenDTO { Token = token };

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{apiBaseUrl}/api/RecuperacionContrasena/validar-token",
                    content
                );

                var responseBody = await response.Content.ReadAsStringAsync();

                var resultado = JsonSerializer.Deserialize<RespuestaRecuperacionDTO>(
                    responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (resultado == null || !resultado.Exito)
                {
                    ViewBag.Error = resultado?.Mensaje ?? "El enlace no es válido o expiró.";
                    return View(new RestablecerContrasenaViewModel());
                }

                return View(new RestablecerContrasenaViewModel { Token = token });
            }
            catch
            {
                ViewBag.Error = "Error al validar el enlace.";
                return View(new RestablecerContrasenaViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> RestablecerContrasena(RestablecerContrasenaViewModel model)
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Restablecer contraseña";
            ViewBag.SubtituloPagina = "Defina una nueva contraseña segura para su cuenta.";

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

                var dto = new RestablecerContrasenaDTO
                {
                    Token = model.Token,
                    NuevaContrasena = model.NuevaContrasena,
                    ConfirmarContrasena = model.ConfirmarContrasena
                };

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{apiBaseUrl}/api/RecuperacionContrasena/restablecer",
                    content
                );

                var responseBody = await response.Content.ReadAsStringAsync();

                var resultado = JsonSerializer.Deserialize<RespuestaRecuperacionDTO>(
                    responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (!response.IsSuccessStatusCode || resultado == null || !resultado.Exito)
                {
                    ViewBag.Error = resultado?.Mensaje ?? "No se pudo restablecer la contraseña.";
                    return View(model);
                }

                TempData["Mensaje"] = resultado.Mensaje;
                return RedirectToAction("IniciarSesion");
            }
            catch
            {
                ViewBag.Error = "Error al restablecer la contraseña.";
                return View(model);
            }
        }
    }
}