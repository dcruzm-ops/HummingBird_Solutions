using Microsoft.AspNetCore.Mvc;

namespace PSA.WebApp.Controllers
{
    public class AdministracionController : Controller
    {
        [HttpGet]
        public IActionResult GestionUsuarios()
        {
            ViewBag.ModuloActivo = "administracion";
            ViewBag.RolActivo = "Administrador";
            ViewBag.TituloPagina = "Gestión de usuarios";
            ViewBag.SubtituloPagina = "Administre accesos, estados y roles del sistema.";
            ViewBag.BreadcrumbActual = "Gestión de usuarios";
            return View();
        }

        [HttpGet]
        public IActionResult ParametrosPago()
        {
            ViewBag.ModuloActivo = "administracion";
            ViewBag.RolActivo = "Administrador";
            ViewBag.TituloPagina = "Parámetros de pago";
            ViewBag.SubtituloPagina = "Defina configuraciones versionadas para el cálculo de pagos.";
            ViewBag.BreadcrumbActual = "Parámetros de pago";
            return View();
        }

        [HttpGet]
        public IActionResult ValidacionCuentasBancarias()
        {
            ViewBag.ModuloActivo = "administracion";
            ViewBag.RolActivo = "Administrador";
            ViewBag.TituloPagina = "Validación de cuentas bancarias";
            ViewBag.SubtituloPagina = "Revise y apruebe cuentas bancarias vinculadas a planes de pago.";
            ViewBag.BreadcrumbActual = "Validación bancaria";
            return View();
        }

        [HttpGet]
        public IActionResult AuditoriaLogs()
        {
            ViewBag.ModuloActivo = "administracion";
            ViewBag.RolActivo = "Administrador";
            ViewBag.TituloPagina = "Auditoría y logs";
            ViewBag.SubtituloPagina = "Consulte trazabilidad de cambios y acciones críticas del sistema.";
            ViewBag.BreadcrumbActual = "Auditoría y logs";
            return View();
        }
    }
}
