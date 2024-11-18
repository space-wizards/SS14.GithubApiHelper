using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using SS14.GithubApiHelper.Configuration;

namespace SS14.GithubApiHelper.Services;

public sealed class RateLimiterService : IDisposable
{
    private readonly ConcurrentDictionary<long, RateLimiter> _rateLimiters = new();
    private readonly TokenBucketRateLimiterOptions _options;

    public RateLimiterService(IConfiguration configuration)
    {
        var config = configuration.GetSection(GithubConfiguration.Name).Get<GithubConfiguration>();
        var limits = config?.RateLimit ?? new GithubConfiguration.RateLimitConfiguration();

        _options = new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = true,
            QueueLimit = limits.QueueLimit,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            TokenLimit = limits.TokenLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            TokensPerPeriod = limits.TokensPerPeriod
        };
    }

    public async Task<bool> Acquire(long id)
    {
        if (!_rateLimiters.TryGetValue(id, out var limiter))
        {
            limiter = new TokenBucketRateLimiter(_options);
            _rateLimiters.TryAdd(id, limiter);
        }

        var lease = await limiter.AcquireAsync();
        return lease.IsAcquired;
    }

    public async Task ClearStaleLimiters()
    {
        List<long> staleLimiters = new();

        foreach ((var id, var limiter) in _rateLimiters)
        {
            if (limiter.IdleDuration > TimeSpan.FromMinutes(30))
                staleLimiters.Add(id);
        }

        foreach (var staleLimiterId in staleLimiters)
        {
            await _rateLimiters[staleLimiterId].DisposeAsync();
            _rateLimiters.Remove(staleLimiterId, out _);
        }
    }

    public void Dispose()
    {
        foreach (var limiter in _rateLimiters.Values)
        {
            limiter.Dispose();
        }
        
        _rateLimiters.Clear();
    }
}