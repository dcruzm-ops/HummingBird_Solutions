using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace PSA.WebApp.Controllers
{
    public class HomeController : AppControllerBase
    {
        public IActionResult Index()
        {
            if (UsuarioAutenticado)
            {
                return RedirectToRoleDashboard();
            }

            return View();
        }

        public IActionResult Equipo()
        {
            if (UsuarioAutenticado)
            {
                return RedirectToRoleDashboard();
            }

            return View();
        }

        public IActionResult Producto()
        {
            if (UsuarioAutenticado)
            {
                return RedirectToRoleDashboard();
            }

            return View();
        }

        [Authorize]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            if (!UsuarioAutenticado)
            {
                return RedirectToAction("IniciarSesion", "Autenticacion", new { returnUrl });
            }

            ViewBag.DashboardAction = GetDashboardActionByRoleClaim(User?.Claims.FirstOrDefault(c => c.Type.EndsWith("role", StringComparison.OrdinalIgnoreCase))?.Value);
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
    }
}
