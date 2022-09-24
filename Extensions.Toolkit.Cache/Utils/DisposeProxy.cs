namespace Extensions.Toolkit.Cache.Utils;

public class DisposeProxy : IDisposable
{
    private readonly Action _onDispose;

    public DisposeProxy(Action onDispose)
    {
        _onDispose = onDispose;
    }

    public void Dispose() => _onDispose();
}
    
public class AsyncDisposeProxy : IAsyncDisposable
{
    private readonly Func<Task> _onDispose;

    public AsyncDisposeProxy(Func<Task> onDispose)
    {
        _onDispose = onDispose;
    }

    public async ValueTask DisposeAsync() => await _onDispose();
}