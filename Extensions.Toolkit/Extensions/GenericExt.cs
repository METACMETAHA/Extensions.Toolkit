using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Extensions.Toolkit;

public static class GenericExt
{
    public static T? Clone<T>(this T source)
    {
        // Don't serialize a null object, simply return the default for that object
        if (ReferenceEquals(source, null))
            return default;
        
        var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
        var serializeSettings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source, serializeSettings), deserializeSettings);
    }
    
    /// <summary>
    /// A T extension method that check if the value is between the minValue and maxValue.
    /// </summary>
    /// <param name="this">The @this to act on.</param>
    /// <param name="minValue">The minimum value.</param>
    /// <param name="maxValue">The maximum value.</param>
    /// <returns>true if the value is between the minValue and maxValue, otherwise false.</returns>
    /// ###
    /// <typeparam name="T">Generic type parameter.</typeparam>
    public static bool Between<T>(this T @this, T minValue, T maxValue) where T : IComparable<T>
    {
        return minValue.CompareTo(@this) == -1 && @this.CompareTo(maxValue) == -1;
    }
    
    public static IEnumerable<string> GetDifferentProperties<T>(this T obj, T comparableObj) where T : class
    {
        var jComparableConfig = JToken.FromObject(comparableObj);
        var difference = JToken.FromObject(obj)
            .FindDiff(jComparableConfig);

        return difference.HasValues ? 
            difference.Properties().Select(x => x.Name) : 
            Enumerable.Empty<string>();
    }
}