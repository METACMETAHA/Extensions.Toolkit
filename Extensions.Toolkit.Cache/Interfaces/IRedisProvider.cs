using StackExchange.Redis;

namespace Extensions.Toolkit.Cache;

public interface IRedisProvider : 
    ICacheProvider, 
    ICacheSubscription, 
    ICacheState, 
    ICacheKeys,
    ICacheConsumer<string>,
    IListProvider
{
    event Action<ConnectionFailedEventArgs> ConnectionRestored;
}

/// <summary>
/// Settings for consume at redis stream. 
/// </summary>
public record RedisConsumeSettings(int PrefetchCount = 100, string Position = "0-0") 
    : CacheConsumeSettings<string>(PrefetchCount, Position);