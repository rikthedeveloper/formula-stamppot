using WebUI.Domain.ObjectStore;
using WebUI.Domain;
using System.Diagnostics.CodeAnalysis;

namespace WebUI.UnitTests.Fakes;

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
