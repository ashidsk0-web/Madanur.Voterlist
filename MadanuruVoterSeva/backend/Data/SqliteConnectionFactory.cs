using System.Data;
using MadanuruVoterSeva.Backend.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace MadanuruVoterSeva.Backend.Data;

public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(IConfiguration configuration)
    {
        var rawConnStr = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=../data/database/madanuru_voter_seva.db";
        
        var builder = new SqliteConnectionStringBuilder(rawConnStr);
        if (!string.IsNullOrEmpty(builder.DataSource) && builder.DataSource != ":memory:")
        {
            builder.DataSource = ResolveDatabasePath(builder.DataSource);
        }

        _connectionString = builder.ConnectionString;
    }

    private static string ResolveDatabasePath(string dataSource)
    {
        var path = Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.Combine(AppContext.BaseDirectory, dataSource);
        path = Path.GetFullPath(path);

        if (!File.Exists(path))
        {
            var devFallback1 = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", dataSource));
            if (File.Exists(devFallback1)) return devFallback1;

            var devFallback2 = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", dataSource));
            if (File.Exists(devFallback2)) return devFallback2;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";
        cmd.ExecuteNonQuery();

        return connection;
    }
}
