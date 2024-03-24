using Microsoft.Data.Sqlite;
using SqlKata;
using SqlKata.Compilers;
using System.Buffers.Text;
using System.Data;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Utils;

namespace WebUI.Domain.ObjectStore.Internal;

static class DefaultCollectionFields
{
    public const string Data = nameof(Data);
    public const string Created = nameof(Created);
    public const string Updated = nameof(Updated);
    public const string Version = nameof(Version);

    public static string[] AllColumns = [Data, Created, Updated, Version];
}

public class DefaultObjectCollection<TEntity>(
    SqliteConnection _conn,
    SqliteTransaction? _transaction,
    string _collection,
    ObjectStoreOptions _storeOptions,
    TimeProvider _timeProvider
    ) : IObjectCollection<TEntity>
    where TEntity : class
{
    static readonly Compiler _compiler = new SqliteCompiler();
    readonly bool _isManagedConnection = _conn.State == ConnectionState.Closed;

    public async Task<IEnumerable<ObjectRecord<TEntity>>> ListAsync(CancellationToken cancellationToken = default)
        => await ListAsync([], cancellationToken);

    public async Task<IEnumerable<ObjectRecord<TEntity>>> ListAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
        => await StreamAsync(specification, cancellationToken).ToArrayAsync(cancellationToken: cancellationToken);

    public async IAsyncEnumerable<ObjectRecord<TEntity>> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in StreamAsync([], cancellationToken))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<ObjectRecord<TEntity>> StreamAsync(ISpecification[] specification, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(
            new Query(_collection).Select(DefaultCollectionFields.AllColumns),
            specification);

        using var command = CreateCommand(_compiler.Compile(query), _conn, _transaction);
        try
        {
            await OpenManagedConnectionAsync(cancellationToken);
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                yield return ReadEntity(reader);
            }
        }
        finally
        {
            await CloseManagedConnectionAsync();
        }
    }
    public async Task<ObjectRecord<TEntity>?> FindAsync(CancellationToken cancellationToken = default) => await FindAsync([], cancellationToken);
    public async Task<ObjectRecord<TEntity>?> FindAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(
            new Query(_collection).Select(DefaultCollectionFields.AllColumns).Limit(1),
            specification);

        using var command = CreateCommand(_compiler.Compile(query), _conn, _transaction);
        try
        {
            await OpenManagedConnectionAsync(cancellationToken);
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return ReadEntity(reader);
            }
        }
        finally
        {
            await CloseManagedConnectionAsync();
        }

        return null;
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken = default) => await CountAsync([], cancellationToken);
    public async Task<long> CountAsync(ISpecification[] specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(
            new Query(_collection).AsCount(),
            specification);

        using var command = CreateCommand(_compiler.Compile(query), _conn, _transaction);
        try
        {
            await OpenManagedConnectionAsync(cancellationToken);
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return reader.GetInt64(0);
            }
        }
        finally
        {
            await CloseManagedConnectionAsync();
        }

        return 0;
    }

    public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => await CountAsync([], cancellationToken) > 0;
    public async Task<bool> ExistsAsync(ISpecification[] specification, CancellationToken cancellationToken = default) => await CountAsync(specification, cancellationToken) > 0;

    public async Task<int> InsertAsync(long key, TEntity entity, CancellationToken cancellationToken = default)
    {
        var q = new Query(_collection)
            .AsInsert(CreateFields());

        using var command = CreateCommand(_compiler.Compile(q), _conn, _transaction);
        try
        {
            await OpenManagedConnectionAsync(cancellationToken);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            await CloseManagedConnectionAsync();
        }

        Dictionary<string, object?> CreateFields()
        {
            var data = SerializeEntity(entity);
            var version = GetObjectVersion(data);
            var created = _timeProvider.GetUtcNow();
            var fields = new Dictionary<string, object?>()
            {
                { DefaultCollectionFields.Data, data },
                { DefaultCollectionFields.Created, created },
                { DefaultCollectionFields.Updated, created },
                { DefaultCollectionFields.Version, version }
            };

            var collectionOptions = _storeOptions.Collections.GetValueOrDefault(typeof(TEntity));
            if (collectionOptions is not null)
            {
                foreach (var customField in collectionOptions.CustomFields)
                {
                    fields.Add(customField.FieldName, customField.GetValue(entity));
                }
            }

            return fields;
        }
    }

    public async Task<int> UpdateAsync(ISpecification[] specification, TEntity entity, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(
            new Query(_collection).AsUpdate(CreateFields()).Limit(1),
            specification);

        using var command = CreateCommand(_compiler.Compile(query), _conn, _transaction);
        try
        {
            await OpenManagedConnectionAsync(cancellationToken);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            await CloseManagedConnectionAsync();
        }

        Dictionary<string, object?> CreateFields()
        {
            var data = SerializeEntity(entity);
            var version = GetObjectVersion(data);
            var updated = _timeProvider.GetUtcNow();
            var fields = new Dictionary<string, object?>()
            {
                { DefaultCollectionFields.Data, data },
                { DefaultCollectionFields.Updated, updated },
                { DefaultCollectionFields.Version, version }
            };

            var collectionOptions = _storeOptions.Collections.GetValueOrDefault(typeof(TEntity));
            if (collectionOptions is not null)
            {
                foreach (var customField in collectionOptions.CustomFields)
                {
                    fields.Add(customField.FieldName, customField.GetValue(entity));
                }
            }

            return fields;
        }
    }

    async Task OpenManagedConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_isManagedConnection)
        {
            await _conn.OpenAsync(cancellationToken);
        }
    }

    async Task CloseManagedConnectionAsync()
    {
        if (_isManagedConnection && _conn.State == ConnectionState.Open)
        {
            await _conn.CloseAsync();
        }
    }

    TEntity DeserializeEntity(string data)
        => JsonSerializer.Deserialize<TEntity>(data, _storeOptions.JsonSerializerOptions) ?? throw new DataFormatIntegrityException();

    string SerializeEntity(TEntity entity)
        => JsonSerializer.Serialize(entity, _storeOptions.JsonSerializerOptions);

    static SqliteCommand CreateCommand(SqlResult sqlResult, SqliteConnection connection, SqliteTransaction? transaction)
    {
        var command = new SqliteCommand(sqlResult.Sql, connection, transaction);
        command.Parameters.AddRange(sqlResult.NamedBindings.Select(b => new SqliteParameter(b.Key, b.Value)));
        return command;
    }

    static Query ApplySpecification(Query query, ISpecification[] specification)
        => specification.Aggregate(query, (q, s) => s.Apply(q));

    ObjectRecord<TEntity> ReadEntity(SqliteDataReader reader) => new(
        DeserializeEntity(reader.GetString(DefaultCollectionFields.Data)),
        reader.GetDateTimeOffset(DefaultCollectionFields.Created),
        reader.GetDateTimeOffset(DefaultCollectionFields.Updated),
        new(reader.GetString(DefaultCollectionFields.Version)));

    static string GetObjectVersion(string data)
        => BitConverter.ToString(Crc32.Hash(Encoding.UTF8.GetBytes(data))).Replace("-", "").ToLowerInvariant();
}

static class DbDataReaderExtensions
{
    public static DateTimeOffset GetDateTimeOffset(this SqliteDataReader reader, string name)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return reader.GetDateTimeOffset(reader.GetOrdinal(name));
    }
}