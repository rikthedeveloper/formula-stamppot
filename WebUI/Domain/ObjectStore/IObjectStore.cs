namespace WebUI.Domain.ObjectStore;

public interface IObjectStore
{
    IReadOnlyObjectCollection<Championship> Championships { get; }
    IReadOnlyObjectCollection<Track> Tracks { get; }
    IReadOnlyObjectCollection<Driver> Drivers { get; }
    Task<IObjectTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IObjectTransaction : IDisposable, IAsyncDisposable
{
    IObjectCollection<Championship> Championships { get; }
    IObjectCollection<Track> Tracks { get; }
    IObjectCollection<Driver> Drivers { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
