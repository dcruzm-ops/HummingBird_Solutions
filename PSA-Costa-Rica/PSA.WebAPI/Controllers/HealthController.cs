using Microsoft.AspNetCore.Mvc;
using PSA.WebAPI.Controllers.Models;

namespace PSA.WebAPI.Controllers
{
    public class HealthController : BaseApiController
    {
        [HttpGet]
        public ActionResult<ApiResponse<object>> Get()
        {
            var data = new
            {
                Status = "OK",
                LocalTime = DateTime.Now,
                Service = "PSA.WebAPI"
            };

            return Ok(ApiResponse<object>.Ok(data, "La API está funcionando correctamente."));
        }

        [HttpGet("error-test")]
        public IActionResult ErrorTest()
        {
            throw new Exception("Error de prueba para validar el middleware global.");
        }
    }
}