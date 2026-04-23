using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs.Pagos;
using PSA.WebApp.Services;
using System.Security.Claims;

namespace PSA.WebApp.Controllers
{
    [Authorize]
    public class PagosController : AppControllerBase
    {
        private readonly HttpClientService _httpClientService;

        public PagosController(HttpClientService httpClientService)
        {
            _httpClientService = httpClientService;
        }

        [HttpGet]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> PlanesPago(int? anio = null, string? estadoPlan = null, string? provincia = null, string? canton = null, string? distrito = null, string? estadoBancario = null)
        {
            ViewBag.ModuloActivo = "pagos";
            ViewBag.RolActivo = "Administrador";
            ViewBag.TituloPagina = "Planes de pago";
            ViewBag.SubtituloPagina = "Listado administrativo completo con filtros operativos.";
            ViewBag.BreadcrumbActual = "Planes de pago";
            ViewBag.Anio = anio;
            ViewBag.EstadoPlan = estadoPlan;
            ViewBag.Provincia = provincia;
            ViewBag.Canton = canton;
            ViewBag.Distrito = distrito;
            ViewBag.EstadoBancario = estadoBancario;

            var query = $"?anio={anio}&estadoPlan={Uri.EscapeDataString(estadoPlan ?? string.Empty)}&provincia={Uri.EscapeDataString(provincia ?? string.Empty)}&canton={Uri.EscapeDataString(canton ?? string.Empty)}&distrito={Uri.EscapeDataString(distrito ?? string.Empty)}&estadoBancario={Uri.EscapeDataString(estadoBancario ?? string.Empty)}";
            var planes = await _httpClientService.GetAsync<List<AdminPaymentPlanDto>>($"api/Pagos/admin/planes{query}") ?? new();
            return View(planes);
        }

        [HttpGet]
        [Authorize(Roles = "1,2")]
        public async Task<IActionResult> DetallePlanPago(int id)
        {
            ViewBag.ModuloActivo = "pagos";
            var rol = User.FindFirstValue(ClaimTypes.Role);
            var idUsuario = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId) ? parsedId : 0;

            if (rol == "2")
            {
                ViewBag.RolActivo = "Dueno";
                ViewBag.BreadcrumbPadreTexto = "Mis pagos";
                ViewBag.BreadcrumbPadreUrl = Url.Action(nameof(PlanesDueno));
                var detalleOwner = await _httpClientService.GetAsync<OwnerPaymentPlanDetailDto>($"api/Pagos/dueno/{idUsuario}/planes/{id}");
                if (detalleOwner == null)
                {
                    TempData["Error"] = "No se encontró el plan solicitado o no pertenece a su usuario.";
                    return RedirectToAction(nameof(PlanesDueno));
                }

                return View("DetallePlanPagoDueno", detalleOwner);
            }

            ViewBag.RolActivo = "Administrador";
            ViewBag.BreadcrumbPadreTexto = "Planes de pago";
            ViewBag.BreadcrumbPadreUrl = Url.Action(nameof(PlanesPago));
            var detalleAdmin = await _httpClientService.GetAsync<AdminPaymentPlanDetailDto>($"api/Pagos/admin/planes/{id}");
            if (detalleAdmin == null)
            {
                TempData["Error"] = "No se encontró el plan solicitado.";
                return RedirectToAction(nameof(PlanesPago));
            }

            return View("DetallePlanPagoAdmin", detalleAdmin);
        }

        [HttpGet]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> PlanesDueno()
        {
            ViewBag.ModuloActivo = "pagos";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Mis pagos";
            ViewBag.SubtituloPagina = "Consulte únicamente los planes de sus fincas aprobadas.";
            ViewBag.BreadcrumbActual = "Mis pagos";

            var idUsuario = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var planes = idUsuario > 0
                ? await _httpClientService.GetAsync<List<OwnerPaymentPlanDto>>($"api/Pagos/dueno/{idUsuario}/planes") ?? new()
                : new List<OwnerPaymentPlanDto>();

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
            var resultado = await _httpClientService.PostWithResultAsync<RegistrarCuentaBancariaDTO, int>("api/Pagos/dueno/cuentas-bancarias", model);
            var idCuenta = resultado.Data;
            TempData[idCuenta > 0 ? "Exito" : "Error"] = idCuenta > 0
                ? "Cuenta bancaria registrada. Queda pendiente de validación por administración."
                : resultado.ErrorMessage ?? "No fue posible registrar la cuenta bancaria.";

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
                ? "Cuenta bancaria asociada correctamente."
                : "No fue posible asociar la cuenta bancaria al plan.";

            return RedirectToAction(nameof(PlanesDueno));
        }
    }
}
