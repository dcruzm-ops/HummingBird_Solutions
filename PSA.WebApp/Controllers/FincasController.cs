using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs;
using PSA.EntidadesDTO.DTOs.Fincas;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "2")]
    public class FincasController : AppControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public FincasController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult RegistrarFinca()
        {
            CargarViewBag();
            CargarCatalogosFormularioFinca();
            return View(new RegistrarFincaDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarFinca(RegistrarFincaDTO dto, List<IFormFile>? archivos)
        {
            CargarViewBag();
            dto.IdPropietario = ObtenerIdUsuarioSesion();
            if (dto.IdPropietario <= 0) return RedirectToAction("IniciarSesion", "Autenticacion");
            if (!ModelState.IsValid)
            {
                CargarCatalogosFormularioFinca();
                return View(dto);
            }

            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PostAsJsonAsync("api/Fincas", dto);
            if (!response.IsSuccessStatusCode)
            {
                TempData["MensajeError"] = "No fue posible registrar la finca.";
                CargarCatalogosFormularioFinca();
                return View(dto);
            }

            var idFinca = await ExtraerIdFincaAsync(response);

            if (idFinca > 0 && archivos != null && archivos.Count > 0)
            {
                var evidenciaResponse = await SubirEvidenciasAsync(client, idFinca, dto.IdPropietario, archivos);
                if (!evidenciaResponse)
                {
                    TempData["MensajeError"] = "La finca se registró, pero no fue posible subir las evidencias.";
                }
            }

            TempData["MensajeExito"] = "Finca registrada correctamente.";
            return RedirectToAction(nameof(MisFincas));
        }

        [HttpGet]
        public async Task<IActionResult> MisFincas()
        {
            CargarListadoViewBag();
            var idPropietario = ObtenerIdUsuarioSesion();
            if (idPropietario <= 0) return RedirectToAction("IniciarSesion", "Autenticacion");

            var client = _httpClientFactory.CreateClient("AuthApi");
            var fincas = await client.GetFromJsonAsync<List<FincaResumenDTO>>($"api/Fincas/mis-fincas")
                ?? new List<FincaResumenDTO>();

            return View(fincas);
        }

        [HttpGet]
        public async Task<IActionResult> DetalleFinca(int? id = null)
        {
            CargarDetalleViewBag();
            var idPropietario = ObtenerIdUsuarioSesion();
            if (idPropietario <= 0) return RedirectToAction("IniciarSesion", "Autenticacion");
            if ((id ?? 0) <= 0) return RedirectToAction(nameof(MisFincas));

            var client = _httpClientFactory.CreateClient("AuthApi");

            FincaDetalleDTO? detalle;
            try
            {
                detalle = await client.GetFromJsonAsync<FincaDetalleDTO>($"api/Fincas/{id}/detalle");
                if (detalle == null)
                {
                    TempData["MensajeError"] = "No se encontró la finca solicitada.";
                    return RedirectToAction(nameof(MisFincas));
                }
            }
            catch (HttpRequestException)
            {
                TempData["MensajeError"] = "No fue posible conectarse con el API para cargar el detalle de la finca.";
                return RedirectToAction(nameof(MisFincas));
            }
            catch (TaskCanceledException)
            {
                TempData["MensajeError"] = "La consulta del detalle de finca tardó demasiado. Intente nuevamente.";
                return RedirectToAction(nameof(MisFincas));
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error al cargar el detalle de la finca.";
                return RedirectToAction(nameof(MisFincas));
            }

            var evidencias = new List<FincaEvidenciaDTO>();
            try
            {
                evidencias = await client.GetFromJsonAsync<List<FincaEvidenciaDTO>>($"api/FincaEvidencias/finca/{id}")
                    ?? new List<FincaEvidenciaDTO>();
            }
            catch
            {
                // No se bloquea el render del detalle si falla la carga de evidencias.
                TempData["MensajeError"] = "No fue posible cargar las evidencias de la finca en este momento.";
            }

            var baseAddress = client.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
            foreach (var evidencia in evidencias)
            {
                if (!string.IsNullOrWhiteSpace(evidencia.UrlDescarga) && evidencia.UrlDescarga.StartsWith('/'))
                {
                    evidencia.UrlDescarga = $"{baseAddress}{evidencia.UrlDescarga}";
                }
            }
            ViewBag.Evidencias = evidencias;

            EstadoRenovacionAnualDTO? estadoRenovacion = null;
            try
            {
                estadoRenovacion = await client.GetFromJsonAsync<EstadoRenovacionAnualDTO>($"api/Fincas/{id}/renovacion-anual/estado");
            }
            catch
            {
                // No bloquea render; si falla, UI mantiene fallback conservador.
            }
            ViewBag.EstadoRenovacion = estadoRenovacion;

            return View(detalle);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenovacionAnual(int idFinca)
        {
            var idPropietario = ObtenerIdUsuarioSesion();
            if (idPropietario <= 0) return RedirectToAction("IniciarSesion", "Autenticacion");
            if (idFinca <= 0)
            {
                TempData["MensajeError"] = "La finca indicada para renovación no es válida.";
                return RedirectToAction(nameof(MisFincas));
            }

            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PostAsync($"api/Fincas/{idFinca}/renovacion-anual", null);
            var body = await response.Content.ReadAsStringAsync();
            string? mensajeApi = null;
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("mensaje", out var m1)) mensajeApi = m1.GetString();
                    if (string.IsNullOrWhiteSpace(mensajeApi) && doc.RootElement.TryGetProperty("Mensaje", out var m2)) mensajeApi = m2.GetString();
                }
                catch
                {
                    // Ignorado: fallback a mensaje estándar.
                }
            }

            TempData[response.IsSuccessStatusCode ? "MensajeExito" : "MensajeError"] = response.IsSuccessStatusCode
                ? (mensajeApi ?? "Renovación anual solicitada correctamente.")
                : (mensajeApi ?? "No fue posible solicitar la renovación anual.");
            return RedirectToAction(nameof(DetalleFinca), new { id = idFinca });
        }

        private static async Task<int> ExtraerIdFincaAsync(HttpResponseMessage response)
        {
            try
            {
                using var contenido = await response.Content.ReadAsStreamAsync();
                using var documento = await JsonDocument.ParseAsync(contenido);
                if (documento.RootElement.TryGetProperty("idFinca", out var idFincaLower))
                {
                    return idFincaLower.GetInt32();
                }

                if (documento.RootElement.TryGetProperty("IdFinca", out var idFincaUpper))
                {
                    return idFincaUpper.GetInt32();
                }
            }
            catch
            {
                // Se ignora para no bloquear flujo principal de registro.
            }

            return 0;
        }

        private static async Task<bool> SubirEvidenciasAsync(HttpClient client, int idFinca, int idUsuario, List<IFormFile> archivos)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(idFinca.ToString()), "idFinca");
            form.Add(new StringContent(idUsuario.ToString()), "cargadoPor");

            foreach (var archivo in archivos.Where(a => a != null && a.Length > 0))
            {
                var contenido = new StreamContent(archivo.OpenReadStream());
                contenido.Headers.ContentType = new MediaTypeHeaderValue(archivo.ContentType ?? "application/octet-stream");
                form.Add(contenido, "archivos", archivo.FileName);
            }

            var response = await client.PostAsync("api/FincaEvidencias/subir", form);
            return response.IsSuccessStatusCode;
        }

        private int ObtenerIdUsuarioSesion() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        private void CargarCatalogosFormularioFinca()
        {
            var pendientes = new[] { "Plana", "Inclinada", "Muy inclinada" };
            var vegetaciones = new[] { "Bosque primario", "Bosque secundario", "Plantación forestal", "Pasto" };
            var usosSuelo = new[] { "Conservación", "Producción forestal", "Agroforestal", "Ganadería", "Mixto" };

            ViewBag.OpcionesPendiente = pendientes;
            ViewBag.OpcionesVegetacion = vegetaciones;
            ViewBag.OpcionesUsoSuelo = usosSuelo;

            ViewBag.CatalogoPendiente = pendientes;
            ViewBag.CatalogoVegetacion = vegetaciones;
            ViewBag.CatalogoUsoSuelo = usosSuelo;
        }

        private void CargarViewBag()
        {
            ViewBag.ModuloActivo = "fincas"; ViewBag.RolActivo = "Dueno"; ViewBag.TituloPagina = "Registrar finca";
            ViewBag.SubtituloPagina = "Complete la información principal de la propiedad para iniciar el proceso.";
            ViewBag.BreadcrumbPadreTexto = "Mis fincas"; ViewBag.BreadcrumbPadreUrl = Url.Action("MisFincas", "Fincas"); ViewBag.BreadcrumbActual = "Registrar finca";
        }
        private void CargarListadoViewBag()
        {
            ViewBag.ModuloActivo = "fincas"; ViewBag.RolActivo = "Dueno"; ViewBag.TituloPagina = "Mis fincas";
            ViewBag.SubtituloPagina = "Consulte el estado de sus propiedades registradas y sus procesos asociados."; ViewBag.BreadcrumbActual = "Mis fincas";
        }
        private void CargarDetalleViewBag()
        {
            ViewBag.ModuloActivo = "fincas"; ViewBag.RolActivo = "Dueno"; ViewBag.TituloPagina = "Detalle de finca";
            ViewBag.SubtituloPagina = "Visualice la información general, evaluación, evidencias y plan de pago.";
            ViewBag.BreadcrumbPadreTexto = "Mis fincas"; ViewBag.BreadcrumbPadreUrl = Url.Action("MisFincas", "Fincas"); ViewBag.BreadcrumbActual = "Detalle de finca";
        }
    }
}
