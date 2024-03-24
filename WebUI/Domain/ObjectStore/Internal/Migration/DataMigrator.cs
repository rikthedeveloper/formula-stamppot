using Microsoft.Data.Sqlite;
using System.Data;
using System.Text;

namespace WebUI.Domain.ObjectStore.Internal.Migration;

public class DataMigrator(SqliteConnection conn, ObjectStoreOptions options) : IHostedService
{
    public async Task MigrateDatabase(CancellationToken cancellationToken = default)
    {
        using var transaction = conn.BeginTransaction(IsolationLevel.Serializable);
        
        try
        {
            var sb = new StringBuilder();
            foreach (var collection in options.Collections.Values)
            {
                sb.AppendLine(GetTableExpression(collection));
            }

            using var command = new SqliteCommand(sb.ToString(), conn, transaction);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    static string GetTableExpression(ObjectCollectionOptions collection)
    {
        var columnsBuilder = new StringBuilder();
        var keyBuilder = new StringBuilder("PRIMARY KEY (");
        foreach(var cf in collection.CustomFields)
        {
            columnsBuilder.AppendFormat("{0} {1}", cf.FieldName, cf.DataType);
            if (cf.NotNull)
            {
                columnsBuilder.Append(" NOT NULL");
            }

            columnsBuilder.Append(',');

            if (cf.IsKey)
            {
                keyBuilder.AppendFormat("{0},", cf.FieldName);
            }
        }
        keyBuilder[^1] = ')';
        columnsBuilder.Append("Data TEXT NOT NULL,Created TEXT NOT NULL,Updated TEXT NOT NULL,Version TEXT NOT NULL");
        return string.Format("CREATE TABLE IF NOT EXISTS {0} ({1}) WITHOUT ROWID;", 
            collection.Name, 
            columnsBuilder.AppendFormat(",{0}", keyBuilder.ToString()).ToString());
    }

    public async Task StartAsync(CancellationToken cancellationToken) => await MigrateDatabase(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
