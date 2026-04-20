using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;
using PSA.WebAPI.Extensions;

namespace PSA.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificacionesController(NotificacionesManager notificacionesManager) : ControllerBase
{
    private readonly NotificacionesManager _notificacionesManager = notificacionesManager;

    [HttpGet("usuario/{idUsuario:int}")]
    public async Task<IActionResult> ObtenerPorUsuario([FromRoute] int idUsuario, [FromQuery] int maximo = 30)
    {
        try
        {
            var actor = GetUserId();
            var destino = IsRole("1") ? idUsuario : actor;
            var notificaciones = await _notificacionesManager.ObtenerPorUsuarioAsync(destino, maximo);
            return Ok(notificaciones);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }

    [HttpPost("usuario/{idUsuario:int}/marcar-leidas")]
    public async Task<IActionResult> MarcarLeidas([FromRoute] int idUsuario)
    {
        try
        {
            var actualizadas = await _notificacionesManager.MarcarLeidasAsync(idUsuario);
            return Ok(new { Actualizadas = actualizadas });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
    }
}
