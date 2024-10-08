﻿using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System.Data;

namespace WebUI.Domain.ObjectStore.Internal;

public class DefaultObjectStore(
    SqliteConnection _conn,
    IOptions<ObjectStoreCollectionOptions> _collections,
    IOptions<ObjectStoreJsonOptions> _jsonOptions,
    TimeProvider _timeProvider) : IObjectStore
{
    IReadOnlyObjectCollection<Championship>? _championships;
    public IReadOnlyObjectCollection<Championship> Championships => _championships ??= CreateCollection<Championship>();
    IReadOnlyObjectCollection<Track>? _tracks;
    public IReadOnlyObjectCollection<Track> Tracks => _tracks ??= CreateCollection<Track>();
    IReadOnlyObjectCollection<Driver>? _drivers;
    public IReadOnlyObjectCollection<Driver> Drivers => _drivers ??= CreateCollection<Driver>();
    IReadOnlyObjectCollection<Event>? _events;
    public IReadOnlyObjectCollection<Event> Events => _events ??= CreateCollection<Event>();
    IReadOnlyObjectCollection<Session>? _sessions;
    public IReadOnlyObjectCollection<Session> Sessions => _sessions ??= CreateCollection<Session>();

    public Task<IObjectTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _conn.BeginTransaction(IsolationLevel.Serializable);
        return Task.FromResult<IObjectTransaction>(new DefaultObjectTransaction(_conn, transaction, _collections.Value, _jsonOptions.Value, _timeProvider));
    }

    DefaultObjectCollection<TEntity> CreateCollection<TEntity>() where TEntity : class
        => new(_conn, null, _collections.Value, _jsonOptions.Value, _timeProvider);
}
