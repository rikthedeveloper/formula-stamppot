using Microsoft.Data.Sqlite;
using System.Data;

namespace WebUI.Domain.ObjectStore.Internal;

public class DefaultObjectStore(SqliteConnection _conn, ObjectStoreOptions _storeOptions, TimeProvider _timeProvider) : IObjectStore
{
    readonly Lazy<IReadOnlyObjectCollection<Championship>> _championships = new(() => new DefaultObjectCollection<Championship>(_conn, null, nameof(Championship), _storeOptions, _timeProvider));
    public IReadOnlyObjectCollection<Championship> Championships => _championships.Value;
    readonly Lazy<IReadOnlyObjectCollection<Track>> _tracks = new(() => new DefaultObjectCollection<Track>(_conn, null, nameof(Track), _storeOptions, _timeProvider));
    public IReadOnlyObjectCollection<Track> Tracks => _tracks.Value;

    public Task<IObjectTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _conn.BeginTransaction(IsolationLevel.Serializable);
        return Task.FromResult<IObjectTransaction>(new DefaultObjectTransaction(_conn, transaction, _storeOptions, _timeProvider));
    }
}
