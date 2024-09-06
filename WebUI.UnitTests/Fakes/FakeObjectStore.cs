using WebUI.Domain.ObjectStore;
using WebUI.Domain;
using System.Diagnostics.CodeAnalysis;
using WebUI.Endpoints.Internal.Specifications;

namespace WebUI.UnitTests.Fakes;
public class FakeObjectStore : IObjectStore
{
    public const string DefaultObjectVersion = "1";

    public FakeObjectStore(IEnumerable<Championship>? championships = null,
        IEnumerable<Track>? tracks = null,
        IEnumerable<Driver>? drivers = null,
        IEnumerable<Event>? events = null,
        IEnumerable<Session>? sessions = null)
    {
        Championships = new FakeObjectCollection<Championship>(ConvertToRecords(championships ?? []));
        Tracks = new FakeObjectCollection<Track>(ConvertToRecords(tracks ?? []));
        Drivers = new FakeObjectCollection<Driver>(ConvertToRecords(drivers ?? []));
        Events = new FakeObjectCollection<Event>(ConvertToRecords(events ?? []));
        Sessions = new FakeObjectCollection<Session>(ConvertToRecords(sessions ?? []));
    }

    IReadOnlyObjectCollection<Championship> IObjectStore.Championships => Championships;
    public IObjectCollection<Championship> Championships { get; set; }
    IReadOnlyObjectCollection<Track> IObjectStore.Tracks => Tracks;
    public IObjectCollection<Track> Tracks { get; set; }
    IReadOnlyObjectCollection<Driver> IObjectStore.Drivers => Drivers;
    public IObjectCollection<Driver> Drivers { get; set; }
    IReadOnlyObjectCollection<Event> IObjectStore.Events => Events;
    public IObjectCollection<Event> Events { get; set; }
    IReadOnlyObjectCollection<Session> IObjectStore.Sessions => Sessions;
    public IObjectCollection<Session> Sessions { get; set; }

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
    public IObjectCollection<Driver> Drivers { get; private set; }
    public IObjectCollection<Event> Events { get; private set; }
    public IObjectCollection<Session> Sessions { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        _objectStore.Championships = new FakeObjectCollection<Championship>(Championships.ListAsync(cancellationToken).Result);
        _objectStore.Tracks = new FakeObjectCollection<Track>(Tracks.ListAsync(cancellationToken).Result);
        _objectStore.Drivers = new FakeObjectCollection<Driver>(Drivers.ListAsync(cancellationToken).Result);
        _objectStore.Events = new FakeObjectCollection<Event>(Events.ListAsync(cancellationToken).Result);
        _objectStore.Sessions = new FakeObjectCollection<Session>(Sessions.ListAsync(cancellationToken).Result);
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

    [MemberNotNull(nameof(Championships)), MemberNotNull(nameof(Tracks)), MemberNotNull(nameof(Drivers)), MemberNotNull(nameof(Events)), MemberNotNull(nameof(Sessions))]
    void Initialize()
    {
        Championships = new FakeObjectCollection<Championship>(_objectStore.Championships.ListAsync().Result);
        Tracks = new FakeObjectCollection<Track>(_objectStore.Tracks.ListAsync().Result);
        Drivers = new FakeObjectCollection<Driver>(_objectStore.Drivers.ListAsync().Result);
        Events = new FakeObjectCollection<Event>(_objectStore.Events.ListAsync().Result);
        Sessions = new FakeObjectCollection<Session>(_objectStore.Sessions.ListAsync().Result);
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

    public Task<int> InsertAsync(T model, CancellationToken cancellationToken = default)
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
