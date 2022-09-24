namespace Extensions.Toolkit;

public static class CollectionExt
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) => source?.Any() != true;
    
    public static IEnumerable<T> Iter<T>(this IEnumerable<T> source, Action<T> action) 
        => source.Select(s =>
        {
            action(s);
            return s;
        });
    
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source) => source.Select((item, index) => (item, index));

    #region Permutations
    /// <summary>
    /// Creates various combinations from nested lists
    /// </summary>
    /// <example>
    /// <returns> Set of combinations between all params </returns>
    /// <code>
    /// string[][] variousArrays = {
    ///     new [] { "name1", "name2", "name3", "name4" },
    ///     new [] { "option1", "option1", "option0", "option0", "option0" },
    ///     new [] { "combination1", "combination2" }
    /// };
    ///
    /// var result = variousArrays.Permutations(_ => _);
    /// </code>
    /// </example>
    public static IEnumerable<TValue[]> Permutations<TKey, TValue>(
        this IEnumerable<TKey> keys, 
        Func<TKey, IEnumerable<TValue>> selector)
    {
        var keyArray = keys.ToArray();
        if (keyArray.Length < 1)
            yield break;
        var values = new TValue[keyArray.Length];
        foreach (var array in Permutations(keyArray, 0, selector, values))
            yield return array;
    }

        
    private static IEnumerable<TValue[]> Permutations<TKey, TValue>(
        TKey[] keys, 
        int index, 
        Func<TKey, IEnumerable<TValue>> selector, 
        TValue[] values)
    {
        TKey key = keys[index];
        foreach (TValue value in selector(key))
        {
            values[index] = value;
            if (index < keys.Length - 1)
            {
                foreach (var array in Permutations(keys, index + 1, selector, values))
                    yield return array;
            }
            else
            {
                yield return values.ToArray();
            }
        }
    }
    #endregion Permutations
}