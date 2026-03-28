using Microsoft.AspNetCore.Mvc;
using PSA.DataAccess;

namespace PSA.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class HealthController : BaseApiController
    {
        public HealthController(DbContextHelper dbContextHelper) : base(dbContextHelper)
        {
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            try
            {
                bool result = _dbContextHelper.TestConnection();

                if (result)
                    return Ok(new { message = "Conexión a SQL Server exitosa" });

                return BadRequest(new { message = "No se pudo conectar a SQL Server" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error al probar la conexión",
                    error = ex.Message
                });
            }
        }
    }
}