using Microsoft.AspNetCore.Mvc;

namespace PSA.WebApp.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password) {

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Debe ingresar correo y contrasena";
                return View();
            }

            if (email == "admin@test.com" && password == "1234")
            {
                HttpContext.Session.SetString("rol", "ADMIN");
                return RedirectToAction("Index", "Admin");
            }

            if (email == "dueno@test.com" && password == "1234")
            {
                HttpContext.Session.SetString("rol", "DUENO");
                return RedirectToAction("Index", "Dueno");
            }

            if (email == "ing@test.com" && password == "1234")
            {
                HttpContext.Session.SetString("rol", "INGENIERO");
                return RedirectToAction("Index", "Ingeniero");
            }

            ViewBag.Error = "Credenciales invalidas";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
