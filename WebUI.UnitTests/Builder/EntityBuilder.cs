using Ease;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace WebUI.UnitTests.Builder;

internal abstract class EntityBuilder<T> : Builder<T>
    where T : class
{
    private ListDictionary? _propertiesCached;
    protected ListDictionary Properties => _propertiesCached ??= (ListDictionary)typeof(Builder<T>).GetField("_properties", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(this)!;

    protected TProp Get<TProp>(Expression<Func<T, TProp>> expression, TProp defaultValue)
    {
        var value = Get(expression);
        return Equals(value, default(TProp)) ? defaultValue : value;
    }

    protected TProp Get<TProp>(string key, TProp defaultValue)
    {
        var value = Get<TProp>(key);
        return Equals(value, default(TProp)) ? defaultValue : value;
    }

    protected override T CreateInstance()
    {
        static ConstructorInfo? findFittingConstructor(IOrderedEnumerable<ConstructorInfo> constructors)
        {
            var properties = typeof(T).GetProperties().Select(p => p.Name).ToHashSet();
            return constructors.FirstOrDefault(c => c.GetParameters().All(p => properties.Contains(p.Name ?? string.Empty, StringComparer.InvariantCultureIgnoreCase)));
        }

        var defaultConstructor = typeof(T).GetConstructor(Type.EmptyTypes);
        if (defaultConstructor is not null)
        {
            return base.CreateInstance();
        }

        var fittingConstructor = findFittingConstructor(typeof(T).GetConstructors().OrderByDescending(c => c.GetParameters().Length)) 
            ?? throw new InvalidOperationException($"No valid constructor found for {typeof(T).Name}");

        var properties = new Dictionary<string, object?>(StringComparer.InvariantCultureIgnoreCase);
        foreach (DictionaryEntry prop in Properties)
        {
            properties[prop.Key.ToString()!] = prop.Value;
        }

        // Make an array of all the parameters that the constructor needs using values from the Properties dictionary, using case insensitive key comparison
        var parameters = fittingConstructor.GetParameters().Select(p => properties[p.Name ?? string.Empty]).ToArray();
        var result = (T)fittingConstructor.Invoke(parameters);

        foreach (var (key, value) in properties)
        {
            var prop = typeof(T).GetProperty(key);
            if (prop?.SetMethod is not null)
                prop.SetValue(result, value);
        }

        return result;
    }
}
