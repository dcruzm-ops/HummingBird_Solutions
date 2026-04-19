using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;

namespace PSA.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificacionesController(NotificacionesManager notificacionesManager) : ControllerBase
{
    private readonly NotificacionesManager _notificacionesManager = notificacionesManager;

    [HttpGet("usuario/{idUsuario:int}")]
    public async Task<IActionResult> ObtenerPorUsuario([FromRoute] int idUsuario, [FromQuery] int maximo = 30)
    {
        try
        {
            var notificaciones = await _notificacionesManager.ObtenerPorUsuarioAsync(idUsuario, maximo);
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
