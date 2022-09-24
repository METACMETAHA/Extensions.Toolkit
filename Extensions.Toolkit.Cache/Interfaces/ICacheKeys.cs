namespace Extensions.Toolkit.Cache;

public interface ICacheKeys
{
    IAsyncEnumerable<string> GetKeys(Func<string, bool> keyFilter);
    IAsyncEnumerable<string> GetKeys(string pattern);
}