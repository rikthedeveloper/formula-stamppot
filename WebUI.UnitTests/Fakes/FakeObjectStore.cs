using WebUI.Domain.ObjectStore;
using WebUI.Domain;

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
