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
        if (File.Exists(dataSource)) return Path.GetFullPath(dataSource);

        var candidatePaths = new[]
        {
            Path.GetFullPath(dataSource),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, dataSource)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../data/database/madanuru_voter_seva.db")),
            @"C:\Voters ID\MadanuruVoterSeva\data\database\madanuru_voter_seva.db",
            @"C:\Voters ID\MadanuruVoterSeva API\data\database\madanuru_voter_seva.db",
            Path.GetFullPath(@"../data/database/madanuru_voter_seva.db"),
            Path.GetFullPath(@"data/database/madanuru_voter_seva.db")
        };

        foreach (var path in candidatePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Default fallback: create under C:\Voters ID\MadanuruVoterSeva\data\database\
        var defaultTarget = @"C:\Voters ID\MadanuruVoterSeva\data\database\madanuru_voter_seva.db";
        var dir = Path.GetDirectoryName(defaultTarget);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return defaultTarget;
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
