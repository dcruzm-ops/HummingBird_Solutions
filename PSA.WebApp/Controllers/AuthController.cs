using Microsoft.AspNetCore.Mvc;

namespace PSA.WebApp.Controllers
{
    public class AuthController : AppControllerBase
    {
        [HttpGet]
        public IActionResult Login()
        {
            return RedirectToAction("IniciarSesion", "Autenticacion");
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            return RedirectToAction("IniciarSesion", "Autenticacion");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}
