using System.Security.Claims;

namespace PSA.WebApp.Services.Security;

public class ApiUserHeadersHandler : DelegatingHandler
{
    private const string HeaderUserId = "X-PSA-UserId";
    private const string HeaderRole = "X-PSA-Role";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiUserHeadersHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = user.FindFirstValue(ClaimTypes.Role);

            if (!string.IsNullOrWhiteSpace(id))
            {
                request.Headers.Remove(HeaderUserId);
                request.Headers.TryAddWithoutValidation(HeaderUserId, id);
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                request.Headers.Remove(HeaderRole);
                request.Headers.TryAddWithoutValidation(HeaderRole, role);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
