using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.Fincas;
using System.Net.Http.Json;
using System.Text.Json;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "2")]
    public class FincasController : Controller
    {
        private readonly FincaDAO _fincaDAO;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public FincasController(
            FincaDAO fincaDAO,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _fincaDAO = fincaDAO;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        public IActionResult RegistrarFinca()
        {
            ViewBag.ModuloActivo = "fincas";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Registrar finca";
            ViewBag.SubtituloPagina = "Complete la información principal de la propiedad.";
            ViewBag.BreadcrumbPadreTexto = "Mis fincas";
            ViewBag.BreadcrumbPadreUrl = Url.Action("MisFincas", "Fincas");
            ViewBag.BreadcrumbActual = "Registrar finca";

            CargarCatalogosFormularioFinca();
            return View(new RegistrarFincaDTO());
        }

        // 🔥 MODIFICADO: ahora recibe archivos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarFinca(RegistrarFincaDTO dto, List<IFormFile>? archivos)
        {
            ViewBag.ModuloActivo = "fincas";
            ViewBag.RolActivo = "Dueno";

            dto.IdPropietario = ObtenerIdUsuarioSesion();

            if (dto.IdPropietario <= 0)
            {
                TempData["MensajeError"] = "Debe iniciar sesión.";
                return RedirectToAction("IniciarSesion", "Autenticacion");
            }

            if (!ModelState.IsValid)
            {
                CargarCatalogosFormularioFinca();
                return View(dto);
            }

            try
            {
                var client = _serviceProvider.GetService<IHttpClientFactory>()?.CreateClient("AuthApi")
                    ?? throw new Exception("HttpClient no disponible");

                var baseUrl = GetApiBaseUrls().First();

                // 🟢 1. Crear finca
                var response = await client.PostAsJsonAsync($"{baseUrl}/api/Fincas", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", error);
                    CargarCatalogosFormularioFinca();
                    return View(dto);
                }

                var idFinca = await ObtenerIdFincaDesdeRespuestaAsync(response);

                // 🟢 2. Subir archivos (si hay)
                if (idFinca > 0 && archivos != null && archivos.Count > 0)
                {
                    await SubirEvidenciasAsync(client, baseUrl, idFinca, dto.IdPropietario, archivos);
                }

                TempData["MensajeExito"] = "Finca registrada correctamente";
                return RedirectToAction(nameof(MisFincas));
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = ex.Message;
                return RedirectToAction(nameof(MisFincas));
            }
        }

        // 🔥 NUEVO: subir archivos
        private async Task SubirEvidenciasAsync(
            HttpClient client,
            string baseUrl,
            int idFinca,
            int idUsuario,
            List<IFormFile> archivos)
        {
            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(idFinca.ToString()), "idFinca");
            form.Add(new StringContent(idUsuario.ToString()), "cargadoPor");

            foreach (var archivo in archivos)
            {
                if (archivo.Length == 0) continue;

                var stream = new StreamContent(archivo.OpenReadStream());
                stream.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(
                        archivo.ContentType ?? "application/octet-stream");

                form.Add(stream, "archivos", archivo.FileName);
            }

            await client.PostAsync($"{baseUrl}/api/FincaEvidencias/subir", form);
        }

        private async Task<int> ObtenerIdFincaDesdeRespuestaAsync(HttpResponseMessage response)
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.TryGetProperty("IdFinca", out var id))
            {
                return id.GetInt32();
            }

            return 0;
        }

        [HttpGet]
        public async Task<IActionResult> MisFincas()
        {
            var id = ObtenerIdUsuarioSesion();

            if (id <= 0)
                return RedirectToAction("IniciarSesion", "Autenticacion");

            var fincas = await _fincaDAO.ObtenerPorPropietarioAsync(id);
            return View(fincas);
        }

        [HttpGet]
        public async Task<IActionResult> DetalleFinca(int id)
        {
            var userId = ObtenerIdUsuarioSesion();

            var finca = await _fincaDAO.ObtenerDetalleAsync(id, userId);

            if (finca == null)
                return RedirectToAction(nameof(MisFincas));

            return View(finca);
        }

        private IEnumerable<string> GetApiBaseUrls()
        {
            yield return "https://localhost:59665";
        }

        private int ObtenerIdUsuarioSesion()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        private void CargarCatalogosFormularioFinca()
        {
            ViewBag.CatalogoPendiente = new List<string> { "Plana", "Inclinada" };
            ViewBag.CatalogoVegetacion = new List<string> { "Bosque primario", "Pasto" };
            ViewBag.CatalogoUsoSuelo = new List<string> { "Conservación", "Ganadería" };
        }
    }
}