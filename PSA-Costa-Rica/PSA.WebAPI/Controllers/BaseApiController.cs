using Microsoft.AspNetCore.Mvc;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
    }
}