using Microsoft.AspNetCore.Mvc;
using PSA.WebApp.Filters;

namespace PSA.WebApp.Controllers
{
    [RoleAuthorize("INGENIERO")]
    public class EvaluacionesController : Controller
    {
        [HttpGet]
        public IActionResult FincasPendientes()
        {
            ViewBag.ModuloActivo = "evaluaciones";
            ViewBag.RolActivo = "Ingeniero";
            ViewBag.TituloPagina = "Fincas registradas o pendientes";
            ViewBag.SubtituloPagina = "Revise propiedades listas para evaluación o seguimiento.";
            ViewBag.BreadcrumbActual = "Fincas pendientes";
            return View();
        }

        [HttpGet]
        public IActionResult NuevaEvaluacion(int? fincaId = null)
        {
            ViewBag.ModuloActivo = "evaluaciones";
            ViewBag.RolActivo = "Ingeniero";
            ViewBag.FincaId = fincaId ?? 1;
            ViewBag.TituloPagina = "Nueva evaluación técnica";
            ViewBag.SubtituloPagina = "Registre visita, observaciones, evidencias y decisión técnica.";
            ViewBag.BreadcrumbPadreTexto = "Fincas pendientes";
            ViewBag.BreadcrumbPadreUrl = Url.Action("FincasPendientes", "Evaluaciones");
            ViewBag.BreadcrumbActual = "Nueva evaluación";
            return View();
        }

        [HttpGet]
        public IActionResult EvaluacionesEnProceso()
        {
            ViewBag.ModuloActivo = "evaluaciones";
            ViewBag.RolActivo = "Ingeniero";
            ViewBag.TituloPagina = "Evaluaciones en proceso";
            ViewBag.SubtituloPagina = "Consulte evaluaciones abiertas y tareas por completar.";
            ViewBag.BreadcrumbActual = "Evaluaciones en proceso";
            return View();
        }

        [HttpGet]
        public IActionResult HistorialEvaluaciones()
        {
            ViewBag.ModuloActivo = "evaluaciones";
            ViewBag.RolActivo = "Ingeniero";
            ViewBag.TituloPagina = "Historial de evaluaciones";
            ViewBag.SubtituloPagina = "Revise evaluaciones finalizadas y sus decisiones técnicas.";
            ViewBag.BreadcrumbActual = "Historial de evaluaciones";
            return View();
        }
    }
}