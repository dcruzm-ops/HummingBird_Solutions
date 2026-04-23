using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.EntidadesDTO.DTOs.Notificaciones;
using PSA.WebApp.Models;
using System.Net.Http.Json;
using System.Security.Claims;

namespace PSA.WebApp.Controllers
{
    [Authorize(Roles = "1,2,3")]
    public class NotificacionesController : AppControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public NotificacionesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var rol = User.FindFirstValue(ClaimTypes.Role) ?? "2";
            var rolActivo = rol switch
            {
                "1" => "Administrador",
                "3" => "Ingeniero",
                _ => "Dueno"
            };

            ViewBag.ModuloActivo = "notificaciones";
            ViewBag.RolActivo = rolActivo;
            ViewBag.TituloPagina = "Notificaciones";
            ViewBag.SubtituloPagina = "Revise avisos del sistema asociados a evaluaciones, cuentas, pagos y acciones administrativas.";
            ViewBag.BreadcrumbActual = "Notificaciones";
            ViewBag.AccionDashboard = rol switch
            {
                "1" => "Administrador",
                "3" => "Ingeniero",
                _ => "Dueno"
            };

            var idUsuario = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            if (idUsuario <= 0)
            {
                TempData["MensajeError"] = "No fue posible identificar su sesión para cargar notificaciones.";
                return View(new NotificacionesViewModel());
            }

            var client = _httpClientFactory.CreateClient("AuthApi");
            var data = await client.GetFromJsonAsync<List<NotificacionDTO>>($"api/Notificaciones/usuario/{idUsuario}?maximo=50") ?? new();
            await client.PostAsync($"api/Notificaciones/usuario/{idUsuario}/marcar-leidas", content: null);

            var model = new NotificacionesViewModel
            {
                Items = data.Select(x => new NotificacionItemViewModel
                {
                    Id = x.Id,
                    Titulo = x.Titulo,
                    Mensaje = x.Mensaje,
                    Tipo = x.Tipo,
                    Leida = x.Leida,
                    Fecha = x.FechaEnvio
                }).ToList()
            };

            return View(model);
        }
    }
}
