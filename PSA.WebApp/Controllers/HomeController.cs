using Microsoft.AspNetCore.Mvc;
using PSA.WebApp.Models;
using System.Diagnostics;

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
