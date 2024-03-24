using Microsoft.Data.Sqlite;
using System.Data;
using WebUI.Domain.ObjectStore.Internal.Extensions;

namespace WebUI.Domain.ObjectStore.Internal;

public interface IDbConnectionProvider
{
    public SqliteConnection GetConnection();
}

internal class ServiceProviderLifetimeDbConnectionProvider : IDbConnectionProvider, IDisposable
{
    readonly string _connectionString;
    SqliteConnection? _connection;

    public ServiceProviderLifetimeDbConnectionProvider()
    {
        _connectionString = "Data Source=:memory:";
    }

    public SqliteConnection GetConnection()
    {
        _connection ??= new SqliteConnection(_connectionString);
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }
        return _connection;
    }

    #region IDisposable implementation

    bool _disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _connection?.Close();
                _connection?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}


internal class ScopedDbConnectionProvider(string connectionString) : IDbConnectionProvider, IDisposable
{
    readonly string _connectionString = connectionString;
    SqliteConnection? _connection;

    public SqliteConnection GetConnection()
    {
        var conn = _connection ??= new SqliteConnection(_connectionString);
        if (!conn.IsOpen())
        {
            conn.Open();
        }

        return conn;
    }

    #region IDisposable implementation

    bool _disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (_connection?.State == ConnectionState.Open)
                {
                    _connection?.Close();
                }

                _connection?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}

