using Ease;
using System.Linq.Expressions;

namespace WebUI.UnitTests.Builder;

internal abstract class EntityBuilder<T> : Builder<T>
    where T : class
{
    protected TProp Get<TProp>(Expression<Func<T, TProp>> expression, TProp defaultValue)
    {
        var value = base.Get(expression);
        return object.Equals(value, default(TProp)) ? defaultValue : value;
    }

    protected TProp Get<TProp>(string key, TProp defaultValue)
    {
        var value = base.Get<TProp>(key);
        return object.Equals(value, default(TProp)) ? defaultValue : value;
    }
}
