using Microsoft.AspNetCore.Mvc;
using PSA.AppCore.Managers;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LandingController : ControllerBase
    {
        private readonly LandingManager _landingManager;

        public LandingController(LandingManager landingManager)
        {
            _landingManager = landingManager;
        }

        [HttpGet("equipo")]
        public async Task<IActionResult> ObtenerEquipo()
        {
            var data = await _landingManager.ObtenerEquipoAsync();
            return Ok(data);
        }

        [HttpGet("producto")]
        public async Task<IActionResult> ObtenerProducto()
        {
            var data = await _landingManager.ObtenerProductoAsync();
            return Ok(data);
        }
    }
}
