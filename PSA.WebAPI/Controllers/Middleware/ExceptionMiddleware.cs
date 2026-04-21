using System.Net;
using System.Text.Json;
using PSA.WebAPI.Controllers.Models;

namespace PSA.WebAPI.Controllers.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = exception switch
            {
                InvalidOperationException => (int)HttpStatusCode.UnprocessableEntity,
                UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var response = new ApiErrorResponse
            {
                Code = context.Response.StatusCode == (int)HttpStatusCode.UnprocessableEntity ? "business_rule_error" : "internal_error",
                Message = context.Response.StatusCode == (int)HttpStatusCode.InternalServerError
                    ? "Ocurrió un error interno en el servidor."
                    : "No fue posible procesar la solicitud.",
                Errors = [exception.Message],
                TraceId = context.TraceIdentifier
            };

            var json = JsonSerializer.Serialize(response);

            await context.Response.WriteAsync(json);
        }
    }
}
