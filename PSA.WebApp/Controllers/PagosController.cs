using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PSA.EntidadesDTO.DTOs.Pagos;
using PSA.WebApp.Services;
using System.Security.Claims;

namespace PSA.WebApp.Controllers
{
    [Authorize]
    public class PagosController : Controller
    {
        private readonly HttpClientService _httpClientService;

        public PagosController(HttpClientService httpClientService)
        {
            _httpClientService = httpClientService;
        }

        [HttpGet]
        [Authorize(Roles = "1")]
        public IActionResult PlanesPago()
        {
            ViewBag.ModuloActivo = "pagos";
            ViewBag.RolActivo = "Administrador";
            ViewBag.TituloPagina = "Planes de pago";
            ViewBag.SubtituloPagina = "Consulte los planes generados y su estado general.";
            ViewBag.BreadcrumbActual = "Planes de pago";
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "1")]
        public IActionResult DetallePlanPago(int? id = null)
        {
            ViewBag.ModuloActivo = "pagos";
            ViewBag.RolActivo = "Administrador";
            ViewBag.PlanPagoId = id ?? 1;
            ViewBag.TituloPagina = "Detalle del plan de pago";
            ViewBag.SubtituloPagina = "Revise cuotas mensuales, estado de pago y atrasos.";
            ViewBag.BreadcrumbPadreTexto = "Planes de pago";
            ViewBag.BreadcrumbPadreUrl = Url.Action("PlanesPago", "Pagos");
            ViewBag.BreadcrumbActual = "Detalle del plan";
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> PlanesDueno()
        {
            ViewBag.ModuloActivo = "pagos";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Planes de pago";
            ViewBag.SubtituloPagina = "Consulte sus planes de pago por finca.";
            ViewBag.BreadcrumbActual = "Planes de pago";

            var idUsuario = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var planes = idUsuario > 0
                ? await _httpClientService.GetAsync<List<PlanPagoResumenDTO>>($"api/Pagos/dueno/{idUsuario}/planes") ?? new()
                : new List<PlanPagoResumenDTO>();
            var cuentas = idUsuario > 0
                ? await _httpClientService.GetAsync<List<CuentaBancariaDuenoDTO>>($"api/Pagos/dueno/{idUsuario}/cuentas-bancarias") ?? new()
                : new List<CuentaBancariaDuenoDTO>();

            ViewBag.CuentasValidadasActivas = cuentas
                .Where(c => string.Equals(c.EstadoValidacion, "Validada", StringComparison.OrdinalIgnoreCase) && c.Activa)
                .ToList();

            return View(planes);
        }

        [HttpGet]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> HistorialPagos()
        {
            ViewBag.ModuloActivo = "pagos";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Historial de pagos";
            ViewBag.SubtituloPagina = "Consulte cuotas históricas y estados de confirmación.";
            ViewBag.BreadcrumbActual = "Historial de pagos";

            var idUsuario = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var historial = idUsuario > 0
                ? await _httpClientService.GetAsync<List<CuotaPlanPagoDTO>>($"api/Pagos/dueno/{idUsuario}/historial") ?? new()
                : new List<CuotaPlanPagoDTO>();

            return View(historial);
        }

        [HttpGet]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> CuentaBancaria()
        {
            ViewBag.ModuloActivo = "pagos";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Cuenta bancaria";
            ViewBag.SubtituloPagina = "Registre y consulte el estado de validación de sus cuentas.";
            ViewBag.BreadcrumbActual = "Cuenta bancaria";

            var idUsuario = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var cuentas = idUsuario > 0
                ? await _httpClientService.GetAsync<List<CuentaBancariaDuenoDTO>>($"api/Pagos/dueno/{idUsuario}/cuentas-bancarias") ?? new()
                : new List<CuentaBancariaDuenoDTO>();

            ViewBag.CuentaNueva = new RegistrarCuentaBancariaDTO { IdUsuario = idUsuario };
            return View(cuentas);
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarCuentaBancaria(RegistrarCuentaBancariaDTO model)
        {
            var idUsuario = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            model.IdUsuario = idUsuario;
            var idCuenta = await _httpClientService.PostAsync<RegistrarCuentaBancariaDTO, int>("api/Pagos/dueno/cuentas-bancarias", model);
            TempData[idCuenta > 0 ? "Exito" : "Error"] = idCuenta > 0
                ? "Cuenta bancaria registrada. Queda pendiente de validación por administración."
                : "No fue posible registrar la cuenta bancaria.";

            return RedirectToAction(nameof(CuentaBancaria));
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsociarCuentaPlan(int idPlanPago, int idCuentaBancaria)
        {
            var idUsuario = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var resultado = await _httpClientService.PutAsync<AsociarCuentaPlanDTO, object>(
                $"api/Pagos/dueno/planes/{idPlanPago}/cuenta-bancaria",
                new AsociarCuentaPlanDTO
                {
                    IdUsuario = idUsuario,
                    IdCuentaBancaria = idCuentaBancaria
                });

            TempData[resultado != null ? "Exito" : "Error"] = resultado != null
                ? "Cuenta bancaria asociada correctamente al plan activo."
                : "No fue posible asociar la cuenta bancaria al plan.";

            return RedirectToAction(nameof(PlanesDueno));
        }
    }
}
