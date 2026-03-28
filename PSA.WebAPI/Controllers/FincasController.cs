using Microsoft.AspNetCore.Mvc;
using PSA.DataAccess.DAO;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FincasController : ControllerBase
    {
        private readonly FincaDAO _fincaDAO;

        public FincasController(FincaDAO fincaDAO)
        {
            _fincaDAO = fincaDAO;
        }

        [HttpGet("mis-fincas")]
        public async Task<IActionResult> ObtenerMisFincas([FromQuery] int idPropietario = 2)
        {
            if (idPropietario <= 0)
            {
                return BadRequest(new { Mensaje = "El idPropietario debe ser mayor a 0." });
            }

            var fincas = await _fincaDAO.ObtenerPorPropietarioAsync(idPropietario);
            return Ok(fincas);
        }

        [HttpGet("{idFinca:int}/detalle")]
        public async Task<IActionResult> ObtenerDetalle([FromRoute] int idFinca, [FromQuery] int idPropietario = 2)
        {
            if (idFinca <= 0 || idPropietario <= 0)
            {
                return BadRequest(new { Mensaje = "Los parámetros idFinca e idPropietario deben ser mayores a 0." });
            }

            var detalle = await _fincaDAO.ObtenerDetalleAsync(idFinca, idPropietario);
            if (detalle == null)
            {
                return NotFound(new { Mensaje = "No se encontró la finca solicitada." });
            }

            return Ok(detalle);
        }
    }
}
