using Microsoft.Data.Sqlite;

namespace WebUI.Domain.ObjectStore.Internal;

public class DefaultObjectTransaction(
    SqliteConnection _conn,
    SqliteTransaction _transaction,
    ObjectStoreCollectionOptions _collections,
    ObjectStoreJsonOptions _jsonOptions,
    TimeProvider _timeProvider) : IObjectTransaction, IDisposable, IAsyncDisposable
{
    bool _isCommitted = false;

    IObjectCollection<Championship>? _championships;
    public IObjectCollection<Championship> Championships => _championships ??= CreateCollection<Championship>();
    IObjectCollection<Track>? _tracks;
    public IObjectCollection<Track> Tracks => _tracks ??= CreateCollection<Track>();
    IObjectCollection<Driver>? _drivers;
    public IObjectCollection<Driver> Drivers => _drivers ??= CreateCollection<Driver>();
    IObjectCollection<Event>? _events;
    public IObjectCollection<Event> Events => _events ??= CreateCollection<Event>();
    IObjectCollection<Session>? _sessions;
    public IObjectCollection<Session> Sessions => _sessions ??= CreateCollection<Session>();

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await _transaction.CommitAsync(cancellationToken);
        _isCommitted = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken) => await _transaction.RollbackAsync(cancellationToken);

    DefaultObjectCollection<TEntity> CreateCollection<TEntity>() where TEntity : class
        => new (_conn, _transaction, _collections, _jsonOptions, _timeProvider);

    #region IDisposable, IAsyncDisposable implementation
    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (!_isCommitted) 
                { 
                    _transaction.Rollback();
                }
                _transaction.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (!_isCommitted)
                {
                    await _transaction.RollbackAsync(CancellationToken.None);
                }
                await _transaction.DisposeAsync();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Do not change this code. Put cleanup code in 'DisposeAsync(bool disposing)' method
        await DisposeAsync(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
