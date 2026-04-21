using Microsoft.Extensions.Caching.Memory;

namespace PSA.WebAPI.Services.Security;

public interface ISecurityThrottleService
{
    bool IsBlocked(string area, string key, out TimeSpan retryAfter);
    void RegisterFailure(string area, string key);
    void RegisterSuccess(string area, string key);
}

public sealed class SecurityThrottleService(IMemoryCache cache, IConfiguration configuration, ILogger<SecurityThrottleService> logger) : ISecurityThrottleService
{
    private readonly IMemoryCache _cache = cache;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<SecurityThrottleService> _logger = logger;

    public bool IsBlocked(string area, string key, out TimeSpan retryAfter)
    {
        var normalized = Normalize(area, key);
        if (_cache.TryGetValue<BlockState>($"sec:block:{normalized}", out var block) && block is not null)
        {
            retryAfter = block.BlockedUntilUtc - DateTime.UtcNow;
            if (retryAfter > TimeSpan.Zero)
            {
                return true;
            }
        }

        retryAfter = TimeSpan.Zero;
        return false;
    }

    public void RegisterFailure(string area, string key)
    {
        var normalized = Normalize(area, key);
        var attemptsKey = $"sec:attempts:{normalized}";
        var maxAttempts = GetInt("SecurityThrottle:MaxAttempts", 5);
        var windowMinutes = GetInt("SecurityThrottle:WindowMinutes", 10);
        var blockMinutes = GetInt("SecurityThrottle:BlockMinutes", 15);

        var state = _cache.GetOrCreate(attemptsKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(windowMinutes);
            return new AttemptState();
        }) ?? new AttemptState();

        state.Count++;
        _cache.Set(attemptsKey, state, TimeSpan.FromMinutes(windowMinutes));

        if (state.Count < maxAttempts)
        {
            return;
        }

        var blockUntil = DateTime.UtcNow.AddMinutes(blockMinutes);
        _cache.Set($"sec:block:{normalized}", new BlockState(blockUntil), blockUntil);
        _cache.Remove(attemptsKey);
        _logger.LogWarning("Bloqueo temporal de seguridad. Área: {Area}. Clave: {Key}. Hasta: {BlockedUntilUtc}.", area, key, blockUntil);
    }

    public void RegisterSuccess(string area, string key)
    {
        var normalized = Normalize(area, key);
        _cache.Remove($"sec:attempts:{normalized}");
        _cache.Remove($"sec:block:{normalized}");
    }

    private int GetInt(string key, int fallback)
        => int.TryParse(_configuration[key], out var value) && value > 0 ? value : fallback;

    private static string Normalize(string area, string key)
        => $"{area.Trim().ToLowerInvariant()}:{(string.IsNullOrWhiteSpace(key) ? "unknown" : key.Trim().ToLowerInvariant())}";

    private sealed class AttemptState
    {
        public int Count { get; set; }
    }

    private sealed record BlockState(DateTime BlockedUntilUtc);
}
