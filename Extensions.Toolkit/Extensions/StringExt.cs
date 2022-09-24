namespace Extensions.Toolkit;

public static class StringExt
{
    /// <summary>
    /// Persistent hashcode even after restart application
    /// </summary>
    public static int GetStableHashCode(this IEnumerable<string>? args, bool caseSensitive = false)
    {
        unchecked
        {
            int hash = 11;
            foreach (string arg in args ?? Enumerable.Empty<string>())
            {
                hash = hash * 3 + GetStableHashCode(arg, caseSensitive);
            }

            return hash;
        }
    }
    
    /// <summary>
    /// Persistent hashcode even after restart application
    /// </summary>
    public static int GetStableHashCode(this string? arg, bool caseSensitive = false)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in arg ?? string.Empty)
            {
                hash = hash * 31 + (!caseSensitive && char.IsUpper(c) ? c : char.ToUpper(c));
            }

            return hash;
        }
    }
}