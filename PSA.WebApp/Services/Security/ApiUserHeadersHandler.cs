using System.Security.Claims;

namespace PSA.WebApp.Services.Security;

public class ApiUserHeadersHandler : DelegatingHandler
{
    private const string ClaimApiToken = "psa_api_token";
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
            var token = user.FindFirst(ClaimApiToken)?.Value;
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
