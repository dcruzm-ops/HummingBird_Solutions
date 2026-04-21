using Microsoft.AspNetCore.Mvc;
using PSA.WebAPI.Controllers.Models;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        protected IActionResult ApiOk<T>(T data, string message = "Operación realizada correctamente.")
            => Ok(ApiResponse<T>.Ok(data, message));

        protected IActionResult ApiCreated<T>(T data, string message = "Recurso creado correctamente.")
            => StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Ok(data, message));

        protected IActionResult ApiError(int statusCode, string code, string message, params string[] errors)
            => StatusCode(statusCode, new ApiErrorResponse
            {
                Code = code,
                Message = message,
                Errors = errors?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [],
                TraceId = HttpContext.TraceIdentifier
            });

        protected IActionResult ApiValidationError(string message, params string[] errors)
            => ApiError(StatusCodes.Status400BadRequest, "validation_error", message, errors);

        protected IActionResult ApiBusinessError(string message, params string[] errors)
            => ApiError(StatusCodes.Status422UnprocessableEntity, "business_rule_error", message, errors);

        protected IActionResult ApiUnauthorizedError(string message = "No autorizado.")
            => ApiError(StatusCodes.Status401Unauthorized, "unauthorized", message);

        protected IActionResult ApiForbiddenError(string message = "No tiene permisos para ejecutar esta acción.")
            => ApiError(StatusCodes.Status403Forbidden, "forbidden", message);

        protected IActionResult ApiNotFoundError(string message)
            => ApiError(StatusCodes.Status404NotFound, "not_found", message);
    }
}
