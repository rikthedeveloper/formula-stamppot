using WebUI.Domain.ObjectStore;
using WebUI.Domain;
using System.Diagnostics.CodeAnalysis;
using WebUI.Endpoints.Internal.Specifications;

namespace WebUI.UnitTests.Fakes;
public class FakeObjectStore : IObjectStore
{
    public const string DefaultObjectVersion = "1";

    public FakeObjectStore(IEnumerable<Championship>? championships = null, IEnumerable<Track>? tracks = null)
    {
        Championships = new FakeObjectCollection<Championship>(ConvertToRecords(championships ?? []));
        Tracks = new FakeObjectCollection<Track>(ConvertToRecords(tracks ?? []));
    }

    public IObjectCollection<Championship> Championships { get; set; }
    public IObjectCollection<Track> Tracks { get; set; }
    IReadOnlyObjectCollection<Championship> IObjectStore.Championships => Championships;
    IReadOnlyObjectCollection<Track> IObjectStore.Tracks => Tracks;

    public Task<IObjectTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = new FakeTransaction(this);
        return Task.FromResult<IObjectTransaction>(transaction);
    }

    static IEnumerable<ObjectRecord<T>> ConvertToRecords<T>(IEnumerable<T> objects)
        => objects.Select(obj => new ObjectRecord<T>(obj, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new ObjectVersion("1")));
}

public class FakeTransaction : IObjectTransaction
{
    private readonly FakeObjectStore _objectStore;

    public FakeTransaction(FakeObjectStore objectStore)
    {
        _objectStore = objectStore;
        Initialize();
    }

    public IObjectCollection<Championship> Championships { get; private set; }
    public IObjectCollection<Track> Tracks { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        _objectStore.Championships = new FakeObjectCollection<Championship>(Championships.ListAsync(cancellationToken).Result);
        _objectStore.Tracks = new FakeObjectCollection<Track>(Tracks.ListAsync(cancellationToken).Result);
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        Initialize();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // No-op in the fake implementation
    }

    public async ValueTask DisposeAsync()
    {
        // No-op in the fake implementation
        await Task.CompletedTask;
    }

    [MemberNotNull(nameof(Championships)), MemberNotNull(nameof(Tracks))]
    void Initialize()
    {
        Championships = new FakeObjectCollection<Championship>(_objectStore.Championships.ListAsync().Result);
        Tracks = new FakeObjectCollection<Track>(_objectStore.Tracks.ListAsync().Result);
    }
}

public class FakeObjectCollection<T> : IObjectCollection<T> where T : class
{
    private readonly List<ObjectRecord<T>> _records;

    public FakeObjectCollection(IEnumerable<ObjectRecord<T>> records)
    {
        _records = records.ToList();
    }

    public Task<IEnumerable<ObjectRecord<T>>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ObjectRecord<T>>>(_records);
    }

    public Task<IEnumerable<ObjectRecord<T>>> ListAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ObjectRecord<T>>>(_records);
    }

    public IAsyncEnumerable<ObjectRecord<T>> StreamAsync(CancellationToken cancellationToken = default)
    {
        return _records.ToAsyncEnumerable();
    }

    public IAsyncEnumerable<ObjectRecord<T>> StreamAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
    {
        return _records.ToAsyncEnumerable();
    }

    public Task<ObjectRecord<T>?> FindAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_records.FirstOrDefault());
    }

    public Task<ObjectRecord<T>?> FindAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_records.FirstOrDefault());
    }

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_records.Any());
    }

    public Task<bool> ExistsAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_records.Any());
    }

    public Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_records.Count);
    }

    public Task<long> CountAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_records.Count);
    }

    public Task<int> InsertAsync(long key, T model, CancellationToken cancellationToken = default)
    {
        _records.Add(new ObjectRecord<T>(model, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new ObjectVersion(FakeObjectStore.DefaultObjectVersion)));
        return Task.FromResult(1);
    }

    public Task<int> UpdateAsync(ISpecification[] specification, T model, CancellationToken cancellationToken = default)
    {
        var recordToUpdate = _records.FirstOrDefault();
        if (recordToUpdate != null)
        {
            if (specification.OfType<VersionMatchSpecification>().Any())
            {
                var versionSpec = specification.OfType<VersionMatchSpecification>().First();
                if (recordToUpdate.Version != versionSpec.Version)
                    return Task.FromResult(0);
            }

            _records[_records.IndexOf(recordToUpdate)] = new ObjectRecord<T>(model, recordToUpdate.Created, DateTimeOffset.UtcNow, new ObjectVersion(FakeObjectStore.DefaultObjectVersion));
            return Task.FromResult(1);
        }
        return Task.FromResult(0);
    }
}
