using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;

namespace Extensions.Toolkit;

public static class MemoryCacheExt
{
    private static readonly Func<MemoryCache, object> GetEntriesCollection = Delegate.CreateDelegate(
        typeof(Func<MemoryCache, object>),
        typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance)?.GetGetMethod(true)!,
        throwOnBindFailure: true) as Func<MemoryCache, object> ?? throw new InvalidOperationException();

    public static IDictionary ToDictionary(this IMemoryCache memoryCache) =>
        (IDictionary)GetEntriesCollection((MemoryCache)memoryCache);
    
    public static IEnumerable GetKeys(this IMemoryCache memoryCache) =>
        ToDictionary(memoryCache).Keys;
    
    public static IEnumerable<T> GetKeys<T>(this IMemoryCache memoryCache) =>
        GetKeys(memoryCache).OfType<T>();
}