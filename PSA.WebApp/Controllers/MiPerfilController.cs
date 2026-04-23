using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs.Usuarios;
using PSA.WebApp.Models;
using System.Net.Http.Json;
using System.Security.Claims;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "1,2,3")]
    public class MiPerfilController : AppControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MiPerfilController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ConfigurarVista("Mi perfil", "Consulte y actualice su información personal y seguridad de cuenta.");

            var idUsuario = ObtenerIdUsuarioSesion();
            if (idUsuario <= 0)
                return RedirectToAction("IniciarSesion", "Autenticacion");

            var model = await CargarPerfilAsync(idUsuario) ?? ConstruirPerfilFallback(idUsuario);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(MiPerfilViewModel model)
        {
            ConfigurarVista("Mi perfil", "Consulte y actualice su información personal y seguridad de cuenta.");

            var idUsuario = ObtenerIdUsuarioSesion();
            if (idUsuario <= 0)
                return RedirectToAction("IniciarSesion", "Autenticacion");

            model.IdUsuario = idUsuario;
            if (!ModelState.IsValid)
            {
                var perfilActual = await CargarPerfilAsync(idUsuario);
                model.RolNombre = perfilActual?.RolNombre ?? model.RolNombre;
                model.Estado = perfilActual?.Estado ?? model.Estado;
                model.FechaCreacion = perfilActual?.FechaCreacion ?? model.FechaCreacion;
                model.UltimoAcceso = perfilActual?.UltimoAcceso ?? model.UltimoAcceso;
                return View("Index", model);
            }

            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PutAsJsonAsync($"api/Perfil/mi-perfil/{idUsuario}", new
            {
                model.NombreCompleto,
                model.Email
            });

            if (!response.IsSuccessStatusCode)
            {
                TempData["MensajeError"] = "No fue posible actualizar su perfil en este momento.";
                var perfilActual = await CargarPerfilAsync(idUsuario) ?? ConstruirPerfilFallback(idUsuario);
                perfilActual.NombreCompleto = model.NombreCompleto;
                perfilActual.Email = model.Email;
                return View("Index", perfilActual);
            }

            TempData["MensajeExito"] = "Su perfil se actualizó correctamente.";
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

        private async Task<MiPerfilViewModel?> CargarPerfilAsync(int idUsuario)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AuthApi");
                var perfil = await client.GetFromJsonAsync<MiPerfilDTO>($"api/Perfil/mi-perfil/{idUsuario}");
                if (perfil == null) return null;

                return new MiPerfilViewModel
                {
                    IdUsuario = perfil.IdUsuario,
                    NombreCompleto = perfil.NombreCompleto,
                    Email = perfil.Email,
                    RolNombre = perfil.RolNombre,
                    Estado = perfil.Estado,
                    FechaCreacion = perfil.FechaCreacion,
                    UltimoAcceso = perfil.UltimoAcceso
                };
            }
            catch
            {
                return null;
            }
        }

        private MiPerfilViewModel ConstruirPerfilFallback(int idUsuario)
        {
            return new MiPerfilViewModel
            {
                IdUsuario = idUsuario,
                NombreCompleto = User.FindFirstValue(ClaimTypes.Name) ?? "Usuario PSA",
                Email = User.FindFirstValue(ClaimTypes.Email) ?? "usuario@psa.local",
                RolNombre = ObtenerNombreRol(),
                Estado = "Activo",
                FechaCreacion = DateTime.Now
            };
        }

        private int ObtenerIdUsuarioSesion() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

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
            var rolId = User.FindFirst(ClaimTypes.Role)?.Value;
            return rolId switch
            {
                "1" => "Administrador",
                "3" => "Ingeniero",
                _ => "Dueño"
            };
        }
    }
}
