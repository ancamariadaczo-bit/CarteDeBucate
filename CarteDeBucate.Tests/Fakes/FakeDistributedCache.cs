using Microsoft.Extensions.Caching.Distributed;

public class FakeDistributedCache : IDistributedCache
{
    private readonly Dictionary<string, byte[]> _entries;
    private readonly Dictionary<string, DistributedCacheEntryOptions> _options = new();

    public FakeDistributedCache(IReadOnlyDictionary<string, byte[]> entries)
    {
        _entries = new Dictionary<string, byte[]>(entries);
    }

    public byte[]? Get(string key)
    {
        throw new NotImplementedException();
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        _entries.TryGetValue(key, out byte[]? value);

        return Task.FromResult(value);
    }

    public void Refresh(string key)
    {
        throw new NotImplementedException();
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public void Remove(string key)
    {
        throw new NotImplementedException();
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        _entries.Remove(key);
        _options.Remove(key);

        return Task.CompletedTask;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        throw new NotImplementedException();
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        _entries[key] = value.ToArray();
        _options[key] = options;

        return Task.CompletedTask;
    }

    public DistributedCacheEntryOptions? GetOptions(string key)
    {
        _options.TryGetValue(key, out DistributedCacheEntryOptions? options);

        return options;
    }
}