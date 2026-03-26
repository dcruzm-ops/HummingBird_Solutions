using Microsoft.AspNetCore.Mvc;

namespace PSA.WebApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Equipo()
        {
            return View();
        }
    }
}