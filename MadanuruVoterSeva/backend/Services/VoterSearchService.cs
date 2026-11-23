using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Dapper;
using MadanuruVoterSeva.Backend.DTOs;
using MadanuruVoterSeva.Backend.Interfaces;

namespace MadanuruVoterSeva.Backend.Services;

public class VoterSearchService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public VoterSearchService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public static string MaskEPIC(string epic)
    {
        if (string.IsNullOrWhiteSpace(epic)) return string.Empty;
        var clean = epic.Trim();
        if (clean.Length <= 4) return "****";
        if (clean.Length <= 6) return clean.Substring(0, 2) + "****";
        // Show first 3 and last 2 characters, mask middle
        return clean.Substring(0, 3) + new string('*', clean.Length - 5) + clean.Substring(clean.Length - 2);
    }

    public async Task<VoterSearchResultDto?> SearchByEpicAsync(string epicNumber)
    {
        if (string.IsNullOrWhiteSpace(epicNumber)) return null;

        // Normalization: trim, uppercase, remove hyphens/spaces
        string normalized = Regex.Replace(epicNumber.Trim().ToUpperInvariant(), @"[\s\-]", "");

        using var connection = _connectionFactory.CreateConnection();

        // Exact match on NormalizedEPIC for active roll version (or latest approved)
        var sql = @"
            SELECT 
                v.VoterId,
                v.RollVersionId,
                v.EPICNumber,
                v.OriginalName,
                v.VerifiedEnglishName,
                v.RelativeName,
                v.VerifiedEnglishRelativeName,
                v.Gender,
                w.WardNumber,
                w.WardName,
                v.GramPanchayatSerialNumber,
                v.AssemblyPartNumber,
                v.AssemblySerialNumber,
                v.HouseNumber,
                v.SourcePageNumber,
                v.VerificationStatus,
                ps.StationName AS PollingStationName,
                ps.StationAddress AS PollingStationAddress,
                COALESCE(ps.VerificationStatus, 'Unverified') AS PollingStationVerificationStatus
            FROM Voter v
            JOIN WardMaster w ON v.WardId = w.WardId
            JOIN RollVersion rv ON v.RollVersionId = rv.RollVersionId
            LEFT JOIN VoterPollingStation vps ON vps.Id = (
                SELECT MIN(a.Id) FROM VoterPollingStation a
                JOIN PollingStation station ON station.PollingStationId = a.PollingStationId
                WHERE a.VoterId = v.VoterId AND a.VerificationStatus = 'Verified'
                  AND station.RollVersionId = v.RollVersionId AND station.VerificationStatus = 'Verified'
                HAVING COUNT(*) = 1
            )
            LEFT JOIN PollingStation ps ON vps.PollingStationId = ps.PollingStationId AND ps.RollVersionId = v.RollVersionId AND ps.VerificationStatus = 'Verified'
            WHERE rv.IsActive = 1 AND v.NormalizedEPIC = @NormalizedEPIC
            LIMIT 1;";

        var row = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { NormalizedEPIC = normalized });
        if (row == null) return null;

        return MapToSearchResult(row);
    }

    public async Task<PagedResult<VoterSearchResultDto>> SearchByNameAsync(string? name, int? wardNumber, int page = 1, int pageSize = 15)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 15;
        long offset = (page - 1L) * pageSize;

        using var connection = _connectionFactory.CreateConnection();

        var whereClauses = new List<string> { "rv.IsActive = 1" };
        var orderBy = "w.WardNumber ASC, v.GramPanchayatSerialNumber ASC";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = CleanName(name).Trim();
            var pattern = "%" + term.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_") + "%";
            var nameMatch = "v.SearchName LIKE @NamePattern ESCAPE '!' OR v.VerifiedEnglishName LIKE @NamePattern ESCAPE '!' OR v.SearchRelativeName LIKE @NamePattern ESCAPE '!' OR v.VerifiedEnglishRelativeName LIKE @NamePattern ESCAPE '!'";
            bool roman = Regex.IsMatch(term, @"^[a-zA-Z]+(?:[ -][a-zA-Z]+)*$");
            var normalizedTerm = roman ? TeluguNameSearch.NormalizeRoman(term) : term;
            if (connection is SqliteConnection sqlite)
            {
                if (roman)
                {
                    sqlite.CreateFunction<string?, string>("romanize_telugu", TeluguNameSearch.Romanize, isDeterministic: true);
                    nameMatch += " OR romanize_telugu(v.SearchName) LIKE @RomanPattern";
                    parameters.Add("RomanPattern", "%" + normalizedTerm + "%");
                }

                if (normalizedTerm.Count(char.IsLetter) >= 5 &&
                    Regex.IsMatch(term, @"^[\p{L}\p{M}]+(?:[ -][\p{L}\p{M}]+)*$"))
                {
                    var typedWords = normalizedTerm.Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    sqlite.CreateFunction<string?, string?, bool>("name_typo_matches", (local, english) =>
                        TeluguNameSearch.HasOneEditPrefix(
                            roman ? TeluguNameSearch.Romanize(local) : local, typedWords) ||
                        (roman && english is not null &&
                            TeluguNameSearch.HasOneEditPrefix(TeluguNameSearch.NormalizeRoman(english), typedWords)),
                        isDeterministic: true);
                    orderBy = $"CASE WHEN ({nameMatch}) THEN 0 ELSE 1 END, " + orderBy;
                    nameMatch += " OR name_typo_matches(v.SearchName, v.VerifiedEnglishName)";
                }
            }
            whereClauses.Add("(" + nameMatch + ")");
            parameters.Add("NamePattern", pattern);
        }

        if (wardNumber.HasValue && wardNumber.Value > 0)
        {
            whereClauses.Add("w.WardNumber = @WardNumber");
            parameters.Add("WardNumber", wardNumber.Value);
        }

        string whereSql = string.Join(" AND ", whereClauses);

        string countSql = $@"
            SELECT COUNT(1)
            FROM Voter v
            JOIN WardMaster w ON v.WardId = w.WardId
            JOIN RollVersion rv ON v.RollVersionId = rv.RollVersionId
            WHERE {whereSql};";

        int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        string selectSql = $@"
            SELECT 
                v.VoterId,
                v.RollVersionId,
                v.EPICNumber,
                v.OriginalName,
                v.VerifiedEnglishName,
                v.RelativeName,
                v.VerifiedEnglishRelativeName,
                v.Gender,
                w.WardNumber,
                w.WardName,
                v.GramPanchayatSerialNumber,
                v.AssemblyPartNumber,
                v.AssemblySerialNumber,
                v.HouseNumber,
                v.SourcePageNumber,
                v.VerificationStatus,
                ps.StationName AS PollingStationName,
                ps.StationAddress AS PollingStationAddress,
                COALESCE(ps.VerificationStatus, 'Unverified') AS PollingStationVerificationStatus
            FROM Voter v
            JOIN WardMaster w ON v.WardId = w.WardId
            JOIN RollVersion rv ON v.RollVersionId = rv.RollVersionId
            LEFT JOIN VoterPollingStation vps ON vps.Id = (
                SELECT MIN(a.Id) FROM VoterPollingStation a
                JOIN PollingStation station ON station.PollingStationId = a.PollingStationId
                WHERE a.VoterId = v.VoterId AND a.VerificationStatus = 'Verified'
                  AND station.RollVersionId = v.RollVersionId AND station.VerificationStatus = 'Verified'
                HAVING COUNT(*) = 1
            )
            LEFT JOIN PollingStation ps ON vps.PollingStationId = ps.PollingStationId AND ps.RollVersionId = v.RollVersionId AND ps.VerificationStatus = 'Verified'
            WHERE {whereSql}
            ORDER BY {orderBy}
            LIMIT @PageSize OFFSET @Offset;";

        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var rows = await connection.QueryAsync<dynamic>(selectSql, parameters);
        var items = rows.Select(MapToSearchResult).ToList();

        return new PagedResult<VoterSearchResultDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<VoterDetailDto?> GetVoterByIdAsync(int voterId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT 
                v.VoterId,
                v.RollVersionId,
                v.EPICNumber,
                v.OriginalName,
                v.VerifiedEnglishName,
                v.RelativeName,
                v.VerifiedEnglishRelativeName,
                v.Gender,
                w.WardNumber,
                w.WardName,
                v.GramPanchayatSerialNumber,
                v.AssemblyPartNumber,
                v.AssemblySerialNumber,
                v.HouseNumber,
                v.SourcePageNumber,
                v.VerificationStatus,
                ps.StationName AS PollingStationName,
                ps.StationAddress AS PollingStationAddress,
                COALESCE(ps.VerificationStatus, 'Unverified') AS PollingStationVerificationStatus,
                sc.Age,
                sc.RelativeLabel,
                sc.AssemblyConstituencyNumber,
                sc.RawCardText,
                CASE WHEN sc.PhotoStatus = 'Embedded' THEN 1 ELSE 0 END AS HasPhoto,
                rv.ElectionName AS RollElectionName,
                rv.VillageName AS RollVillageName,
                rv.PublicationDate AS RollPublicationDate
            FROM Voter v
            JOIN WardMaster w ON v.WardId = w.WardId
            JOIN RollVersion rv ON v.RollVersionId = rv.RollVersionId
            LEFT JOIN VoterPollingStation vps ON vps.Id = (
                SELECT MIN(a.Id) FROM VoterPollingStation a
                JOIN PollingStation station ON station.PollingStationId = a.PollingStationId
                WHERE a.VoterId = v.VoterId AND a.VerificationStatus = 'Verified'
                  AND station.RollVersionId = v.RollVersionId AND station.VerificationStatus = 'Verified'
                HAVING COUNT(*) = 1
            )
            LEFT JOIN PollingStation ps ON vps.PollingStationId = ps.PollingStationId AND ps.RollVersionId = v.RollVersionId AND ps.VerificationStatus = 'Verified'
            LEFT JOIN VoterSourceCard sc ON sc.VoterId = v.VoterId AND sc.SourcePageNumber = v.SourcePageNumber
            WHERE v.VoterId = @VoterId AND rv.IsActive = 1
            LIMIT 1;";

        var row = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { VoterId = voterId });
        if (row == null) return null;

        var baseDto = MapToSearchResult(row);
        string? rawCardText = ((string?)row.RawCardText)?.Replace("\0", "");
        var sourceGender = Regex.Match(rawCardText ?? "", @":[^\S\r\n]*\d{1,3}[^\S\r\n]+[^:\r\n]+:[^\S\r\n]*([^\r\n]+)");
        return new VoterDetailDto
        {
            VoterId = baseDto.VoterId,
            RollVersionId = baseDto.RollVersionId,
            MaskedEPIC = baseDto.MaskedEPIC,
            OriginalName = baseDto.OriginalName,
            VerifiedEnglishName = baseDto.VerifiedEnglishName,
            RelativeName = baseDto.RelativeName,
            VerifiedEnglishRelativeName = baseDto.VerifiedEnglishRelativeName,
            Gender = sourceGender.Success ? CleanName(sourceGender.Groups[1].Value) : baseDto.Gender,
            WardNumber = baseDto.WardNumber,
            WardName = baseDto.WardName,
            GramPanchayatSerialNumber = baseDto.GramPanchayatSerialNumber,
            AssemblyPartNumber = baseDto.AssemblyPartNumber,
            AssemblySerialNumber = baseDto.AssemblySerialNumber,
            HouseNumber = baseDto.HouseNumber,
            SourcePageNumber = baseDto.SourcePageNumber,
            VerificationStatus = baseDto.VerificationStatus,
            PollingStationName = baseDto.PollingStationName,
            PollingStationAddress = baseDto.PollingStationAddress,
            PollingStationVerificationStatus = baseDto.PollingStationVerificationStatus,
            EpicNumber = (string)row.EPICNumber,
            Age = (int?)row.Age,
            RelativeLabel = row.RelativeLabel is string label ? CleanName(label) : null,
            AssemblyConstituencyNumber = (int?)row.AssemblyConstituencyNumber,
            RawCardText = rawCardText,
            HasPhoto = (long)row.HasPhoto == 1,
            RollElectionName = (string)row.RollElectionName,
            RollVillageName = (string)row.RollVillageName,
            RollPublicationDate = (string?)row.RollPublicationDate
        };
    }

    public async Task<(byte[] PhotoData, string PhotoMimeType)?> GetVoterPhotoAsync(int voterId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
            SELECT sc.PhotoData, sc.PhotoMimeType
            FROM VoterSourceCard sc
            JOIN Voter v ON v.VoterId = sc.VoterId AND v.SourcePageNumber = sc.SourcePageNumber
            JOIN RollVersion rv ON rv.RollVersionId = v.RollVersionId AND rv.IsActive = 1
            WHERE sc.VoterId = @VoterId AND sc.PhotoStatus = 'Embedded';", new { VoterId = voterId });

        return row is null ? null : ((byte[])row.PhotoData, (string)row.PhotoMimeType);
    }

    internal static bool HasNameDamage(string? value) =>
        value?.IndexOfAny(['\0', '\uFFFD']) >= 0;

    internal static string CleanName(string value) =>
        Regex.Replace(value.Replace("\0", "").Replace("\uFFFD", "").Normalize(), @"\s+", " ").Trim();

    internal static VoterSearchResultDto MapToSearchResult(dynamic row)
    {
        string epic = (string)row.EPICNumber;
        return new VoterSearchResultDto
        {
            VoterId = (int)row.VoterId,
            RollVersionId = (int)row.RollVersionId,
            MaskedEPIC = MaskEPIC(epic),
            OriginalName = CleanName((string)row.OriginalName),
            VerifiedEnglishName = (string?)row.VerifiedEnglishName,
            RelativeName = row.RelativeName is string relative ? CleanName(relative) : null,
            VerifiedEnglishRelativeName = (string?)row.VerifiedEnglishRelativeName,
            Gender = (string?)row.Gender,
            WardNumber = (int)row.WardNumber,
            WardName = (string?)row.WardName,
            GramPanchayatSerialNumber = (int)row.GramPanchayatSerialNumber,
            AssemblyPartNumber = (int?)row.AssemblyPartNumber,
            AssemblySerialNumber = (int?)row.AssemblySerialNumber,
            HouseNumber = (string?)row.HouseNumber,
            SourcePageNumber = (int)row.SourcePageNumber,
            VerificationStatus = HasNameDamage((string?)row.OriginalName) || HasNameDamage((string?)row.RelativeName)
                ? "NameReview" : (string)row.VerificationStatus,
            PollingStationName = (string?)row.PollingStationName,
            PollingStationAddress = (string?)row.PollingStationAddress,
            PollingStationVerificationStatus = (string)row.PollingStationVerificationStatus
        };
    }
}
