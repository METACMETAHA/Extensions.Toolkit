namespace Extensions.Toolkit.Cache;

public record CacheConsumeSettings<TStreamPosition>(int PrefetchCount, TStreamPosition Position);

public interface ICacheConsumer<TStreamPosition>
{
    IObservable<TEntity> ConsumeStream<TEntity>(
        string streamName,
        CacheConsumeSettings<TStreamPosition> consumeSettings,
        CancellationToken token);

    bool TryConsumeStream<TEntity>(
        string streamName,
        CacheConsumeSettings<string> consumeSettings,
        CancellationToken token,
        out IObservable<TEntity> stream);
}