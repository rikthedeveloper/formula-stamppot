namespace WebUI.Domain.ObjectStore.Internal;

public class ObjectCollectionOptions<TEntity>() : ObjectCollectionOptions(typeof(TEntity))
    where TEntity : class
{
    public ObjectCollectionOptions<TEntity> AddField(string fieldname, string dataType, Func<TEntity, object?> getValue, bool notNull = true, bool isKey = false, bool isIndexed = false)
    {
        object? getValueWrapper(object entity) => entity is TEntity typedEntity ? getValue(typedEntity) : null;
        base.AddField(fieldname, dataType, getValueWrapper, notNull, isKey, isIndexed);
        return this;
    }

    public ObjectCollectionOptions<TEntity> AddKey(string fieldname, string dataType, Func<TEntity, object?> getValue)
        => AddField(fieldname, dataType, getValue, isKey: true, notNull: true);

    public ObjectCollectionOptions<TEntity> AddKey<T>(string fieldname, Func<TEntity, T> getValue)
        => AddField(fieldname, getValue, isKey: true, notNull: true);

    public ObjectCollectionOptions<TEntity> AddField<T>(string fieldname, Func<TEntity, T> getValue, bool notNull = true, bool isKey = false, bool isIndexed = false)
    {
        var dataType = typeof(T);
        string dataTypeName;
        if (dataType == typeof(int) || dataType == typeof(short))
        {
            dataTypeName = "INTEGER";
        }
        else if (dataType == typeof(long))
        {
            dataTypeName = "BIGINT";
        }
        else if (dataType == typeof(string))
        {
            dataTypeName = "TEXT";
        }
        else
        {
            throw new NotSupportedException();
        }

        object? getValueWrapper(TEntity entity) => getValue(entity) is object obj ? obj : null;
        return AddField(fieldname, dataTypeName, getValueWrapper, notNull, isKey, isIndexed);
    }
}

public class ObjectCollectionOptions(Type type)
{
    public record class CustomField(string FieldName, string DataType, Func<object, object?> GetValue, bool NotNull, bool IsKey, bool IsIndexed);
    readonly List<CustomField> _fields = [];

    public Type Type { get; } = type;
    public string Name { get; } = type.Name;
    public IReadOnlyList<CustomField> KeyFields => _fields.Where(f => f.IsKey).ToList();
    public IReadOnlyList<CustomField> IndexedFields => _fields.Where(f => f.IsIndexed).ToList();
    public IReadOnlyList<CustomField> CustomFields => _fields;

    protected void AddField(string fieldName, string dataType, Func<object, object?> getValue, bool notNull = true, bool isKey = false, bool isIndexed = false)
    {
        _fields.Add(new CustomField(fieldName, dataType, getValue, notNull, isKey, isIndexed));
    }
}

public class ObjectStoreCollectionOptions
{
    readonly Dictionary<Type, ObjectCollectionOptions> _collections = [];
    public IReadOnlyList<ObjectCollectionOptions> Collections => _collections.Values.ToList();

    public ObjectStoreCollectionOptions ConfigureCollection<TEntity>(Action<ObjectCollectionOptions<TEntity>> configure)
        where TEntity : class
    {
        var opts = new ObjectCollectionOptions<TEntity>();
        configure(opts);
        _collections.Add(typeof(TEntity), opts);
        return this;
    }

    public ObjectCollectionOptions<TEntity> Get<TEntity>()
        where TEntity : class
        => _collections.TryGetValue(typeof(TEntity), out var collection) ? (ObjectCollectionOptions<TEntity>)collection : throw new ArgumentException();
}