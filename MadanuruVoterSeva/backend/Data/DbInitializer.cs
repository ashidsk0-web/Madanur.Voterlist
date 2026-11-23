using System.Data;
using Dapper;
using MadanuruVoterSeva.Backend.Interfaces;
using Microsoft.Extensions.Logging;

namespace MadanuruVoterSeva.Backend.Data;

public class DbInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(IDbConnectionFactory connectionFactory, ILogger<DbInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInformation("Initializing SQLite Database schema...");

        using var connection = _connectionFactory.CreateConnection();

        // 1. Create tables and indexes
        var schemaPath = Path.GetFullPath(@"../data/database/schema.sql");
        string schemaSql;
        if (File.Exists(schemaPath))
        {
            schemaSql = File.ReadAllText(schemaPath);
        }
        else
        {
            schemaSql = GetDefaultSchema();
        }

        connection.Execute(schemaSql);
        using (var transaction = connection.BeginTransaction())
        {
            var columns = connection.Query<string>("SELECT name FROM pragma_table_info('Voter');", transaction: transaction).ToHashSet();
            foreach (var column in new[] { "SearchName", "SearchRelativeName" })
            {
                if (!columns.Contains(column))
                    connection.Execute($"ALTER TABLE Voter ADD COLUMN {column} TEXT;", transaction: transaction);
            }

            var pending = connection.Query<(long Id, string Name, string? Relative)>(
                "SELECT VoterId, OriginalName, RelativeName FROM Voter WHERE SearchName IS NULL;",
                transaction: transaction);
            connection.Execute("""
                UPDATE Voter SET SearchName = @Name, SearchRelativeName = @Relative WHERE VoterId = @Id;
                """, pending.Select(row => new {
                    row.Id,
                    Name = Services.VoterSearchService.CleanName(row.Name),
                    Relative = row.Relative is null ? null : Services.VoterSearchService.CleanName(row.Relative)
                }), transaction);
            connection.Execute("""
                CREATE INDEX IF NOT EXISTS IX_Voter_Roll_EPIC ON Voter(RollVersionId, NormalizedEPIC);
                CREATE INDEX IF NOT EXISTS IX_Voter_Roll_Ward_Serial ON Voter(RollVersionId, WardId, GramPanchayatSerialNumber);
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Roll_Active ON RollVersion(IsActive) WHERE IsActive = 1;
                """, transaction: transaction);
            transaction.Commit();
        }

        _logger.LogInformation("Database tables and indexes verified successfully.");

    }

    private static string GetDefaultSchema()
    {
        return @"
            CREATE TABLE IF NOT EXISTS RollVersion (
                RollVersionId INTEGER PRIMARY KEY AUTOINCREMENT,
                ElectionName TEXT NOT NULL,
                VillageName TEXT NOT NULL,
                DistrictName TEXT NOT NULL,
                PublicationDate TEXT,
                QualifyingDate TEXT,
                SourceFileName TEXT NOT NULL,
                TotalPages INTEGER NOT NULL DEFAULT 0,
                VerificationStatus TEXT NOT NULL DEFAULT 'Unverified',
                IsActive INTEGER NOT NULL DEFAULT 0,
                CreatedDate TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
            );

            CREATE TABLE IF NOT EXISTS WardMaster (
                WardId INTEGER PRIMARY KEY AUTOINCREMENT,
                RollVersionId INTEGER NOT NULL,
                WardNumber INTEGER NOT NULL,
                WardName TEXT,
                FOREIGN KEY (RollVersionId) REFERENCES RollVersion(RollVersionId) ON DELETE CASCADE,
                UNIQUE (RollVersionId, WardNumber)
            );

            CREATE TABLE IF NOT EXISTS Voter (
                VoterId INTEGER PRIMARY KEY AUTOINCREMENT,
                RollVersionId INTEGER NOT NULL,
                EPICNumber TEXT NOT NULL,
                NormalizedEPIC TEXT NOT NULL,
                OriginalName TEXT NOT NULL,
                VerifiedEnglishName TEXT,
                RelativeName TEXT,
                VerifiedEnglishRelativeName TEXT,
                Gender TEXT,
                WardId INTEGER NOT NULL,
                GramPanchayatSerialNumber INTEGER NOT NULL,
                AssemblyPartNumber INTEGER,
                AssemblySerialNumber INTEGER,
                HouseNumber TEXT,
                SourcePageNumber INTEGER NOT NULL,
                VerificationStatus TEXT NOT NULL DEFAULT 'Unverified',
                CreatedDate TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                FOREIGN KEY (RollVersionId) REFERENCES RollVersion(RollVersionId) ON DELETE CASCADE,
                FOREIGN KEY (WardId) REFERENCES WardMaster(WardId) ON DELETE RESTRICT
            );

            CREATE TABLE IF NOT EXISTS VoterSourceCard (
                VoterId INTEGER PRIMARY KEY REFERENCES Voter(VoterId) ON DELETE CASCADE,
                PdfSha256 TEXT NOT NULL,
                SourcePageNumber INTEGER NOT NULL,
                RawCardText TEXT NOT NULL,
                Age INTEGER,
                RelativeLabel TEXT,
                AssemblyConstituencyNumber INTEGER,
                PhotoStatus TEXT NOT NULL CHECK (PhotoStatus IN ('Embedded', 'Missing', 'Ambiguous')),
                PhotoData BLOB,
                PhotoMimeType TEXT,
                PhotoWidth INTEGER,
                PhotoHeight INTEGER,
                CHECK ((PhotoStatus = 'Embedded' AND PhotoData IS NOT NULL AND PhotoMimeType IS NOT NULL
                        AND PhotoWidth > 0 AND PhotoHeight > 0)
                       OR (PhotoStatus <> 'Embedded' AND PhotoData IS NULL AND PhotoMimeType IS NULL
                           AND PhotoWidth IS NULL AND PhotoHeight IS NULL))
            );

            CREATE TABLE IF NOT EXISTS PollingStation (
                PollingStationId INTEGER PRIMARY KEY AUTOINCREMENT,
                RollVersionId INTEGER NOT NULL,
                StationCode TEXT NOT NULL,
                StationName TEXT NOT NULL,
                StationAddress TEXT,
                VerificationSource TEXT,
                VerificationStatus TEXT NOT NULL DEFAULT 'Unverified',
                FOREIGN KEY (RollVersionId) REFERENCES RollVersion(RollVersionId) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS VoterPollingStation (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                VoterId INTEGER NOT NULL,
                PollingStationId INTEGER NOT NULL,
                AssignmentSource TEXT,
                VerificationStatus TEXT NOT NULL DEFAULT 'Unverified',
                FOREIGN KEY (VoterId) REFERENCES Voter(VoterId) ON DELETE CASCADE,
                FOREIGN KEY (PollingStationId) REFERENCES PollingStation(PollingStationId) ON DELETE CASCADE,
                UNIQUE (VoterId, PollingStationId)
            );

            CREATE TABLE IF NOT EXISTS VoterImportStaging (
                StagingId INTEGER PRIMARY KEY AUTOINCREMENT,
                RollVersionId INTEGER NOT NULL,
                ExtractedData TEXT NOT NULL,
                SourcePageNumber INTEGER NOT NULL,
                ValidationStatus TEXT NOT NULL DEFAULT 'Pending',
                ValidationMessage TEXT,
                FOREIGN KEY (RollVersionId) REFERENCES RollVersion(RollVersionId) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS AppUser (
                UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                Role TEXT NOT NULL DEFAULT 'Admin',
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedDate TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
            );

            CREATE TABLE IF NOT EXISTS AuditLog (
                AuditId INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER,
                Action TEXT NOT NULL,
                Timestamp TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
                Outcome TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES AppUser(UserId) ON DELETE SET NULL
            );
        ";
    }
}
