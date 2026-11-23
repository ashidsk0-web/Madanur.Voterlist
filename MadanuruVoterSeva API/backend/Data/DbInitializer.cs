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
        _logger.LogInformation("Database tables and indexes verified successfully.");

        // 2. Seed default users if none exist
        int userCount = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM AppUser;");
        if (userCount == 0)
        {
            _logger.LogInformation("Seeding initial administrator and desk user accounts...");

            var adminHash = BCrypt.Net.BCrypt.HashPassword("MadanuruAdmin@2026");
            var deskHash = BCrypt.Net.BCrypt.HashPassword("MadanuruDesk@2026");

            var insertSql = @"
                INSERT INTO AppUser (Username, PasswordHash, Role, IsActive)
                VALUES (@Username, @PasswordHash, @Role, @IsActive);";

            connection.Execute(insertSql, new[]
            {
                new { Username = "admin", PasswordHash = adminHash, Role = "Admin", IsActive = 1 },
                new { Username = "deskuser", PasswordHash = deskHash, Role = "DeskUser", IsActive = 1 }
            });

            _logger.LogInformation("Default users seeded: admin, deskuser.");
        }
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
                Role TEXT NOT NULL DEFAULT 'DeskUser',
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
