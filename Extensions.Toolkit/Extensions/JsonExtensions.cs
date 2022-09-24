using Newtonsoft.Json.Linq;

namespace Newtonsoft.Json;

public static class JsonExtensions
{
    private const string OldState = "old";
    private const string NewState = "new";
    private const string SequenceChanged = "sequence changed";

    public static T? SafeDeserializeObject<T>(this string obj) =>
        TryParseJson<T>(obj, out T? result) ? result : default;

    public static bool TryParseJson<T>(this string obj, out T? result)
    {
        try
        {
            result = JsonConvert.DeserializeObject<T>(obj);
            return true;
        }
        catch (Exception)
        {
            result = default;
            return false;
        }
    }

    public static JObject FindDiff(this JToken thisJToken, JToken compareJToken)
    {
        ArgumentNullException.ThrowIfNull(nameof(thisJToken));
        ArgumentNullException.ThrowIfNull(nameof(compareJToken));
        var diff = new JObject();
        if (JToken.DeepEquals(thisJToken, compareJToken)) return diff;

        switch (thisJToken.Type)
        {
            case JTokenType.Object:
            {
                var current = thisJToken as JObject;
                var model = compareJToken as JObject;
                var addedKeys = ExceptProps(current!, model!);
                var removedKeys = ExceptProps(model!, current!);
                var unchangedKeys = current.Properties()
                    .Where(c => JToken.DeepEquals(c.Value, compareJToken[c.Name]))
                    .Select(c => c.Name);

                WriteDiff(addedKeys, OldState, thisJToken);
                WriteDiff(removedKeys, NewState, compareJToken);

                var potentiallyModifiedKeys =
                    current.Properties().Select(c => c.Name).Except(addedKeys).Except(unchangedKeys);
                foreach (var k in potentiallyModifiedKeys)
                {
                    var foundDiff = FindDiff(current[k], model[k]);
                    if (foundDiff.HasValues) diff[k] = foundDiff;
                }
                
                break;
                
                string[] ExceptProps(JObject left, JObject right) =>
                    left.Properties().Select(c => c.Name)
                        .Except(right.Properties().Select(c => c.Name))
                        .ToArray();

                void WriteDiff(string[] keys, string state, JToken source)
                {
                    foreach (var k in keys)
                    {
                        diff![k] = new JObject
                        {
                            [state] = source[k]
                        };
                    }
                }
            }
            
            case JTokenType.Array:
            {
                var current = thisJToken as JArray;
                var model = compareJToken as JArray;
                var plus = new JArray(current.Except(model, new JTokenEqualityComparer()));
                var minus = new JArray(model.Except(current, new JTokenEqualityComparer()));
                var sequenceChanged = !current.SequenceEqual(model, new JTokenEqualityComparer());
                if (sequenceChanged) diff[SequenceChanged] = true;
                if (plus.HasValues) diff[OldState] = plus;
                if (minus.HasValues) diff[NewState] = minus;
                
                break;
            }
                
            default:
                diff[OldState] = thisJToken;
                diff[NewState] = compareJToken;
                break;
        }

        return diff;
    }

}