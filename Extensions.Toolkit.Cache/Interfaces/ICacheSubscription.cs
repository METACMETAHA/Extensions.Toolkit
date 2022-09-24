using System.Threading.Tasks.Dataflow;

namespace Extensions.Toolkit.Cache;

public interface ICacheSubscription
{
    Task PubAsync<T>(string channel, T value);

    Task PubManyAsync<T>(string channel, IReadOnlyDictionary<string, T> values);

    Task<T?> WaitOne<T>(string channel);

    Task<IAsyncDisposable> SubAsync<T>(string channel, ActionBlock<T> action);

    Task UnSubAsync(string channel);
}