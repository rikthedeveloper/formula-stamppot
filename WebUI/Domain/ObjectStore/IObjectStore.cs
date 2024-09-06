namespace WebUI.Domain.ObjectStore;

public interface IObjectStore
{
    IReadOnlyObjectCollection<Championship> Championships { get; }
    IReadOnlyObjectCollection<Track> Tracks { get; }
    IReadOnlyObjectCollection<Driver> Drivers { get; }
    IReadOnlyObjectCollection<Event> Events { get; }
    IReadOnlyObjectCollection<Session> Sessions { get; }
    Task<IObjectTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IObjectTransaction : IDisposable, IAsyncDisposable
{
    IObjectCollection<Championship> Championships { get; }
    IObjectCollection<Track> Tracks { get; }
    IObjectCollection<Driver> Drivers { get; }
    IObjectCollection<Event> Events { get; }
    IObjectCollection<Session> Sessions { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
