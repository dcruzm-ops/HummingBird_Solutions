using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "1,2,3")]
    public class MiPerfilController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ConfigurarVista("Mi perfil", "Consulte su información personal y opciones de cuenta.");
            return View();
        }

        [HttpGet]
        public IActionResult Editar()
        {
            ConfigurarVista("Editar mis datos", "Actualice su información de contacto y preferencia de notificaciones.");
            ViewBag.BreadcrumbPadreTexto = "Mi perfil";
            ViewBag.BreadcrumbPadreUrl = Url.Action("Index", "MiPerfil");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(string? nombreCompleto, string? correo)
        {
            TempData["MensajeExito"] = "Sus datos fueron actualizados correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult CambiarContrasena()
        {
            ConfigurarVista("Cambiar contraseña", "Actualice su contraseña de acceso de forma segura.");
            ViewBag.BreadcrumbPadreTexto = "Mi perfil";
            ViewBag.BreadcrumbPadreUrl = Url.Action("Index", "MiPerfil");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarContrasena(string? contrasenaActual, string? nuevaContrasena, string? confirmacion)
        {
            TempData["MensajeExito"] = "La contraseña se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult InactivarCuenta()
        {
            ConfigurarVista("Inactivar cuenta", "Solicite la inactivación de su cuenta en el sistema.");
            ViewBag.BreadcrumbPadreTexto = "Mi perfil";
            ViewBag.BreadcrumbPadreUrl = Url.Action("Index", "MiPerfil");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult InactivarCuenta(string? motivo)
        {
            TempData["MensajeExito"] = "Se registró la solicitud de inactivación de cuenta.";
            return RedirectToAction(nameof(Index));
        }

        private void ConfigurarVista(string titulo, string subtitulo)
        {
            ViewBag.ModuloActivo = "mi-perfil";
            ViewBag.RolActivo = ObtenerNombreRol();
            ViewBag.TituloPagina = titulo;
            ViewBag.SubtituloPagina = subtitulo;
            ViewBag.BreadcrumbActual = titulo;
        }

        private string ObtenerNombreRol()
        {
            var rolId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            return rolId switch
            {
                "1" => "Administrador",
                "3" => "Ingeniero",
                _ => "Dueño"
            };
        }
    }
}
