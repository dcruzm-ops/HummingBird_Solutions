using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PSA.WebApp.Services;

public class HttpClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public HttpClientService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var client = _httpClientFactory.CreateClient("AuthApi");
        var response = await client.GetAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        var client = _httpClientFactory.CreateClient("AuthApi");
        var response = await client.PostAsJsonAsync(endpoint, data, JsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        if (typeof(TResponse) == typeof(bool))
        {
            var body = await response.Content.ReadAsStringAsync();
            if (bool.TryParse(body, out var parsed))
            {
                return (TResponse)(object)parsed;
            }
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        var client = _httpClientFactory.CreateClient("AuthApi");
        var content = new StringContent(JsonSerializer.Serialize(data, JsonOptions), Encoding.UTF8, "application/json");
        var response = await client.PutAsync(endpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        if (typeof(TResponse) == typeof(bool))
        {
            var body = await response.Content.ReadAsStringAsync();
            if (bool.TryParse(body, out var parsed))
            {
                return (TResponse)(object)parsed;
            }
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    public async Task<TResponse?> DeleteAsync<TResponse>(string endpoint)
    {
        var client = _httpClientFactory.CreateClient("AuthApi");
        var response = await client.DeleteAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        if (typeof(TResponse) == typeof(bool))
        {
            var body = await response.Content.ReadAsStringAsync();
            if (bool.TryParse(body, out var parsed))
            {
                return (TResponse)(object)parsed;
            }
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }
}
