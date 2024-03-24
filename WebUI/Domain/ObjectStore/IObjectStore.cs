namespace WebUI.Domain.ObjectStore;

public interface IObjectStore
{
    IReadOnlyObjectCollection<Championship> Championships { get; }
    IReadOnlyObjectCollection<Track> Tracks { get; }
    Task<IObjectTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IObjectTransaction : IDisposable, IAsyncDisposable
{
    IObjectCollection<Championship> Championships { get; }
    IObjectCollection<Track> Tracks { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
