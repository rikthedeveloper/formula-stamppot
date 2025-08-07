namespace WebUI.Domain.ObjectStore;

public interface IReadOnlyObjectStore
{
    IReadOnlyObjectCollection<Championship> Championships { get; }
    IReadOnlyObjectCollection<Track> Tracks { get; }
    IReadOnlyObjectCollection<Driver> Drivers { get; }
    IReadOnlyObjectCollection<Event> Events { get; }
    IReadOnlyObjectCollection<Session> Sessions { get; }
}

public interface IObjectStore : IReadOnlyObjectStore
{
    Task<IObjectTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IObjectTransaction : IReadOnlyObjectStore, IDisposable, IAsyncDisposable
{
    new IObjectCollection<Championship> Championships { get; }
    new IObjectCollection<Track> Tracks { get; }
    new IObjectCollection<Driver> Drivers { get; }
    new IObjectCollection<Event> Events { get; }
    new IObjectCollection<Session> Sessions { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
