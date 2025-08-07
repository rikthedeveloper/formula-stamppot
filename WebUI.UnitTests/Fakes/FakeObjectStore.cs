using WebUI.Domain.ObjectStore;
using WebUI.Domain;

namespace WebUI.UnitTests.Fakes;
public class FakeObjectStore : IObjectStore
{
    public static ObjectVersion DefaultObjectVersion { get; } = new("1");

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

    public IObjectCollection<Championship> Championships { get; set; }
    public IObjectCollection<Track> Tracks { get; set; }
    public IObjectCollection<Driver> Drivers { get; set; }
    public IObjectCollection<Event> Events { get; set; }
    public IObjectCollection<Session> Sessions { get; set; }

    IReadOnlyObjectCollection<Championship> IReadOnlyObjectStore.Championships => Championships;
    IReadOnlyObjectCollection<Track> IReadOnlyObjectStore.Tracks => Tracks;
    IReadOnlyObjectCollection<Driver> IReadOnlyObjectStore.Drivers => Drivers;
    IReadOnlyObjectCollection<Event> IReadOnlyObjectStore.Events => Events;
    IReadOnlyObjectCollection<Session> IReadOnlyObjectStore.Sessions => Sessions;

    public Task<IObjectTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = new FakeTransaction(this);
        return Task.FromResult<IObjectTransaction>(transaction);
    }

    static IEnumerable<ObjectRecord<T>> ConvertToRecords<T>(IEnumerable<T> objects)
        where T : class
        => objects.Select(obj => new ObjectRecord<T>(obj, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new ObjectVersion("1")));
}
