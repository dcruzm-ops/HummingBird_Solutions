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

    public async Task<ApiResult<TResponse>> PostWithResultAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        var client = _httpClientFactory.CreateClient("AuthApi");
        var response = await client.PostAsJsonAsync(endpoint, data, JsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            return new ApiResult<TResponse>
            {
                IsSuccess = false,
                ErrorMessage = await ExtractErrorMessageAsync(response)
            };
        }

        return new ApiResult<TResponse>
        {
            IsSuccess = true,
            Data = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions)
        };
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

    private static async Task<string?> ExtractErrorMessageAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("mensaje", out var mensajeMin))
            {
                return mensajeMin.GetString();
            }

            if (doc.RootElement.TryGetProperty("Mensaje", out var mensajeMay))
            {
                return mensajeMay.GetString();
            }
        }
        catch (JsonException)
        {
            // Intencional: si la respuesta no es JSON se usa el fallback.
        }

        return payload;
    }
}

public class ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
}
