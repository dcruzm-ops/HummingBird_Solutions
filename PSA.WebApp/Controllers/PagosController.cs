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
    }
}
