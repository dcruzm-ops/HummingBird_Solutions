using Microsoft.AspNetCore.Mvc;

namespace PSA.WebApp.Controllers
{
    public class HomeController : AppControllerBase
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Equipo()
        {
            return View();
        }

        public IActionResult Producto()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}