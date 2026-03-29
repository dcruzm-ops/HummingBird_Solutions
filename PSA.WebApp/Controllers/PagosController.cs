using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace PSA.WebApp.Controllers
{
    [Authorize]
    public class PagosController : Controller
    {
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
        public IActionResult HistorialPagos()
        {
            ViewBag.ModuloActivo = "pagos";
            ViewBag.RolActivo = "Dueno";
            ViewBag.TituloPagina = "Historial de pagos";
            ViewBag.SubtituloPagina = "Consulte cuotas históricas y estados de confirmación.";
            ViewBag.BreadcrumbActual = "Historial de pagos";
            return View();
        }
    }
}
