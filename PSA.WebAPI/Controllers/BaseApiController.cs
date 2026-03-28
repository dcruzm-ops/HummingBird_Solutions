using Microsoft.AspNetCore.Mvc;
using PSA.DataAccess;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        protected readonly DbContextHelper _dbContextHelper;

        public BaseApiController(DbContextHelper dbContextHelper)
        {
            _dbContextHelper = dbContextHelper;
        }
    }
}