using Microsoft.Data.Sqlite;
using System.Data;

namespace WebUI.Domain.ObjectStore.Internal.Extensions;

internal static class SqliteConnectionExtensions
{
    public static bool IsOpen(this SqliteConnection conn) => conn.State == ConnectionState.Open;
    public static bool IsClosed(this SqliteConnection conn) => conn.State == ConnectionState.Closed;
}
