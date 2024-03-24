using SqlKata;

namespace WebUI.Domain.ObjectStore;

public record struct ObjectVersion(string VersionTag)
{
    public override readonly string ToString() => VersionTag;
    public override readonly int GetHashCode() => VersionTag.GetHashCode();

    public static implicit operator string(ObjectVersion objectVersion) => objectVersion.VersionTag;
}

public record class ObjectRecord(
    DateTimeOffset Created,
    DateTimeOffset Updated,
    ObjectVersion Version);

public record class ObjectRecord<TObject>(
    TObject Object,
    DateTimeOffset Created,
    DateTimeOffset Updated,
    ObjectVersion Version) : ObjectRecord(Created, Updated, Version)
{
    public static implicit operator TObject(ObjectRecord<TObject> objectRecord)
        => objectRecord.Object;
}

public interface ISpecification
{
    Query Apply(Query query);
}



public interface IReadOnlyObjectCollection<TEntity> 
    where TEntity : class
{
    Task<IEnumerable<ObjectRecord<TEntity>>> ListAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ObjectRecord<TEntity>>> ListAsync(ISpecification[] specification, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ObjectRecord<TEntity>> StreamAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<ObjectRecord<TEntity>> StreamAsync(ISpecification[] specification, CancellationToken cancellationToken = default);
    Task<ObjectRecord<TEntity>?> FindAsync(CancellationToken cancellationToken = default);
    Task<ObjectRecord<TEntity>?> FindAsync(ISpecification[] specification, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(ISpecification[] specification, CancellationToken cancellationToken = default);
    Task<long> CountAsync(CancellationToken cancellationToken = default);
    Task<long> CountAsync(ISpecification[] specification, CancellationToken cancellationToken = default);
}
