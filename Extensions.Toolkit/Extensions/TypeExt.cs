using System.Reflection;

namespace Extensions.Toolkit;

public static class TypeExt
{
    public static IEnumerable<PropertyInfo> GetPropertiesRecursive(this Type type)
    {
        var props = type.GetTypeInfo().DeclaredProperties;
        if (type.BaseType != null)
            props = props.Concat(GetPropertiesRecursive(type.BaseType));

        return props;
    }
        
    public static object? GetPropertyValue(this object source, string propertyName)
    {
        PropertyInfo? property= source.GetType().GetProperties()
            .FirstOrDefault(x => x.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        return property?.GetValue(source, null) ?? default;
    }
        
    public static Dictionary<string, object?> AsDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
    {
        return source.GetType().GetProperties(bindingAttr).ToDictionary
        (
            propInfo => propInfo.Name,
            propInfo => propInfo.GetValue(source, null)
        );
    }
}