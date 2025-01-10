using Ease;
using System.Collections.Specialized;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace WebUI.UnitTests.Builder;

internal abstract class EntityBuilder<T, TBuilder> : Builder<T> where TBuilder : EntityBuilder<T, TBuilder>
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

    protected new TBuilder With<TProp>(Expression<Func<T, TProp>> expression, TProp value) => (TBuilder)base.With(expression, value);
    protected new TBuilder With<TProp, TBuilder2>(Expression<Func<T, TProp>> expression, TProp value) where TProp : IBuilder<TBuilder2> => (TBuilder)base.With(expression, value);
    protected new TBuilder With<TProp>(string key, TProp value) => (TBuilder)base.With(key, value);
    protected new TBuilder With<TProp, TBuilder2>(string key, TProp value) where TProp : IBuilder<TBuilder2> => (TBuilder)base.With(key, value);
    protected new TBuilder ForMany<TProp, TBuilder2>(Expression<Func<T, IEnumerable<TProp>>> expression, params TBuilder2[] values) where TBuilder2 : IBuilder<TProp> => (TBuilder)base.ForMany(expression, values);
    protected new TBuilder IgnoreProperty<TProp>(Expression<Func<T, TProp>> expression) => (TBuilder)base.IgnoreProperty(expression);
    protected new TBuilder IgnoreProperty<TProp>(string key) => (TBuilder)base.IgnoreProperty<TProp>(key);

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
