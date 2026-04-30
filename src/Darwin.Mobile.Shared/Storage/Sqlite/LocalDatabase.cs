using Microsoft.Maui.Storage;
using SQLite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Storage.Sqlite;

/// <summary>
/// Owns the shared SQLite database connection used by the mobile apps.
/// </summary>
public sealed class LocalDatabase
{
    private static readonly Lazy<bool> ProviderInitialized = new(InitializeProvider);
    private readonly Lazy<Task<SQLiteAsyncConnection>> _connectionFactory;

    /// <summary>
    /// Initializes a new database owner with lazy connection creation.
    /// </summary>
    public LocalDatabase()
    {
        _ = ProviderInitialized.Value;
        _connectionFactory = new Lazy<Task<SQLiteAsyncConnection>>(CreateConnectionAsync);
    }

    /// <summary>
    /// Gets the asynchronous connection for the local mobile database.
    /// </summary>
    public Task<SQLiteAsyncConnection> GetConnectionAsync() => _connectionFactory.Value;

    private static bool InitializeProvider()
    {
        SQLitePCL.Batteries_V2.Init();
        return true;
    }

    private static Task<SQLiteAsyncConnection> CreateConnectionAsync()
    {
        var appDataDirectory = FileSystem.AppDataDirectory;
        Directory.CreateDirectory(appDataDirectory);

        var databasePath = Path.Combine(appDataDirectory, "darwin.mobile.local.db3");
        var flags =
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.FullMutex;

        return Task.FromResult(new SQLiteAsyncConnection(databasePath, flags));
    }
}
