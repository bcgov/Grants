using Microsoft.Extensions.Caching.Distributed;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;

/// <summary>
/// Wraps an <see cref="IDistributedCache"/> (typically <c>RedisCache</c>) and survives
/// <see cref="ObjectDisposedException"/> after a <see cref="ResettableConnectionMultiplexer"/>
/// recreation.
///
/// When <c>RedisCache</c> is first used it caches the <c>IDatabase</c> it obtained from the
/// multiplexer factory. After the multiplexer is recreated (and the old inner is disposed), every
/// subsequent <c>RedisCache</c> operation throws <c>ObjectDisposedException</c> because the cached
/// <c>IDatabase</c> points to the disposed multiplexer. This wrapper intercepts that exception,
/// instantiates a fresh <c>RedisCache</c> via the supplied factory (which calls the
/// <see cref="ResettableConnectionMultiplexer"/>'s <c>GetDatabase()</c> and obtains a valid
/// <c>IDatabase</c> from the new inner multiplexer), then retries the operation.
/// </summary>
public sealed class ResettableDistributedCache : IDistributedCache
{
#pragma warning disable CS0420 // volatile field passed by-ref to Interlocked — safe
    private volatile IDistributedCache _inner;
#pragma warning restore CS0420
    private readonly Func<IDistributedCache> _factory;
    private readonly ILogger<ResettableDistributedCache> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ResettableDistributedCache(Func<IDistributedCache> factory, ILogger<ResettableDistributedCache> logger)
    {
        _factory = factory;
        _logger = logger;
        _inner = factory();
    }

    private async Task RefreshInnerAsync()
    {
        // Wait up to 5 s for any concurrent refresh to finish; both caller and waiter then use the new inner.
        if (!await _refreshLock.WaitAsync(5000))
        {
            _logger.LogDebug("ResettableDistributedCache: refresh lock timeout — concurrent refresh already running");
            return;
        }
        try
        {
#pragma warning disable CS0420
            Interlocked.Exchange(ref _inner, _factory());
#pragma warning restore CS0420
            _logger.LogInformation("ResettableDistributedCache: inner RedisCache replaced after ObjectDisposedException");
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void RefreshInner()
    {
        if (!_refreshLock.Wait(5000))
        {
            _logger.LogDebug("ResettableDistributedCache: refresh lock timeout (sync) — concurrent refresh already running");
            return;
        }
        try
        {
#pragma warning disable CS0420
            Interlocked.Exchange(ref _inner, _factory());
#pragma warning restore CS0420
            _logger.LogInformation("ResettableDistributedCache: inner RedisCache replaced (sync) after ObjectDisposedException");
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    // ── IDistributedCache ─────────────────────────────────────────────────────

    public byte[]? Get(string key)
    {
        try { return _inner.Get(key); }
        catch (ObjectDisposedException) { RefreshInner(); return _inner.Get(key); }
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        try { return await _inner.GetAsync(key, token); }
        catch (ObjectDisposedException) { await RefreshInnerAsync(); return await _inner.GetAsync(key, token); }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        try { _inner.Set(key, value, options); }
        catch (ObjectDisposedException) { RefreshInner(); _inner.Set(key, value, options); }
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        try { await _inner.SetAsync(key, value, options, token); }
        catch (ObjectDisposedException) { await RefreshInnerAsync(); await _inner.SetAsync(key, value, options, token); }
    }

    public void Refresh(string key)
    {
        try { _inner.Refresh(key); }
        catch (ObjectDisposedException) { RefreshInner(); _inner.Refresh(key); }
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        try { await _inner.RefreshAsync(key, token); }
        catch (ObjectDisposedException) { await RefreshInnerAsync(); await _inner.RefreshAsync(key, token); }
    }

    public void Remove(string key)
    {
        try { _inner.Remove(key); }
        catch (ObjectDisposedException) { RefreshInner(); _inner.Remove(key); }
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        try { await _inner.RemoveAsync(key, token); }
        catch (ObjectDisposedException) { await RefreshInnerAsync(); await _inner.RemoveAsync(key, token); }
    }
}
