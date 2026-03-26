using Microsoft.AspNetCore.Mvc;

namespace PSA.WebApp.Controllers
{
    public class AutenticacionController : Controller
    {
        [HttpGet]
        public IActionResult IniciarSesion()
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Iniciar sesión";
            ViewBag.SubtituloPagina = "Acceda al sistema PSA Costa Rica con sus credenciales.";
            return View();
        }

        [HttpGet]
        public IActionResult RegistroUsuario()
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Registro de usuario";
            ViewBag.SubtituloPagina = "Cree su cuenta para registrar fincas y dar seguimiento a sus procesos.";
            return View();
        }

        [HttpGet]
        public IActionResult RecuperarContrasena()
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TituloPagina = "Recuperar contraseña";
            ViewBag.SubtituloPagina = "Ingrese su correo electrónico para iniciar el proceso de recuperación.";
            return View();
        }

        [HttpGet]
        public IActionResult RestablecerContrasena(string? token = null)
        {
            ViewBag.EsAutenticacion = true;
            ViewBag.TokenRecuperacion = token ?? "TOKEN-DE-EJEMPLO";
            ViewBag.TituloPagina = "Restablecer contraseña";
            ViewBag.SubtituloPagina = "Defina una nueva contraseña segura para su cuenta.";
            return View();
        }
    }
}
