namespace ClipShelf.Core.Caching;

public static class MemoryCacheExtensions
{
    // Caches the task, not the result: concurrent callers share one load, and failures are evicted.
    // The load must not depend on a caller's token; callers use .WaitAsync(ct) to stop waiting.
    public static Task<T> GetOrLoadAsync<T>(this IMemoryCache cache, string key,
        Func<Task<T>> load, TimeSpan timeToLive, long size = 1)
    {
        var lazy = cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = timeToLive;
            entry.Size = size;
            return new Lazy<Task<T>>(async () =>
            {
                try
                {
                    return await load().ConfigureAwait(false);
                }
                catch
                {
                    cache.Remove(key);
                    throw;
                }
            });
        })!;
        return lazy.Value;
    }
}
