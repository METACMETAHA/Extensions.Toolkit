using Newtonsoft.Json.Serialization;

namespace Extensions.Toolkit.Cache;

public interface ICacheProvider
{
    Task SetAsync<T>(string key, T value, TimeSpan ttl);

    Task SetAsync<T>(string key, T value, TimeSpan ttl, IContractResolver contractResolver);

    Task SetManyAsync<T>(IReadOnlyDictionary<string, T> values, TimeSpan ttl);

    Task DeleteManyAsync(IEnumerable<string> keys);
        
    Task<T> GetAsync<T>(string key);

    Task DeleteAsync(string key);
}

public interface IListProvider
{
    Task ListLeftPush<T>(string key, T item);
    Task ListLeftPush<T>(string key, T item, IContractResolver contractResolver);
    Task<T[]> ListRange<T>(string key, long start = 0, long stop = -1);
    Task ListTrim(string key, long start = 0, long stop = -1);
}