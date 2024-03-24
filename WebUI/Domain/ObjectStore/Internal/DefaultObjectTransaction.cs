using Microsoft.Data.Sqlite;

namespace WebUI.Domain.ObjectStore.Internal;

public class DefaultObjectTransaction(
    SqliteConnection _conn,
    SqliteTransaction _transaction,
    ObjectStoreOptions _storeOptions,
    TimeProvider _timeProvider) : IObjectTransaction, IDisposable, IAsyncDisposable
{
    bool _isCommitted = false;

    readonly Lazy<IObjectCollection<Championship>> _championships = new(() => new DefaultObjectCollection<Championship>(_conn, _transaction, nameof(Championship), _storeOptions, _timeProvider));
    public IObjectCollection<Championship> Championships => _championships.Value;
    readonly Lazy<IObjectCollection<Track>> _tracks = new(() => new DefaultObjectCollection<Track>(_conn, _transaction, nameof(Track), _storeOptions, _timeProvider));
    public IObjectCollection<Track> Tracks => _tracks.Value;

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await _transaction.CommitAsync(cancellationToken);
        _isCommitted = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken) => await _transaction.RollbackAsync(cancellationToken);

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
