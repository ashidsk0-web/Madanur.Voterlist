using System.Text.RegularExpressions;
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
            LEFT JOIN VoterPollingStation vps ON v.VoterId = vps.VoterId
            LEFT JOIN PollingStation ps ON vps.PollingStationId = ps.PollingStationId
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
        int offset = (page - 1) * pageSize;

        using var connection = _connectionFactory.CreateConnection();

        var whereClauses = new List<string> { "rv.IsActive = 1" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var pattern = $"%{name.Trim()}%";
            whereClauses.Add("(v.OriginalName LIKE @NamePattern OR v.VerifiedEnglishName LIKE @NamePattern OR v.RelativeName LIKE @NamePattern OR v.VerifiedEnglishRelativeName LIKE @NamePattern)");
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
            LEFT JOIN VoterPollingStation vps ON v.VoterId = vps.VoterId
            LEFT JOIN PollingStation ps ON vps.PollingStationId = ps.PollingStationId
            WHERE {whereSql}
            ORDER BY w.WardNumber ASC, v.GramPanchayatSerialNumber ASC
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
                rv.ElectionName AS RollElectionName,
                rv.VillageName AS RollVillageName,
                rv.PublicationDate AS RollPublicationDate
            FROM Voter v
            JOIN WardMaster w ON v.WardId = w.WardId
            JOIN RollVersion rv ON v.RollVersionId = rv.RollVersionId
            LEFT JOIN VoterPollingStation vps ON v.VoterId = vps.VoterId
            LEFT JOIN PollingStation ps ON vps.PollingStationId = ps.PollingStationId
            WHERE v.VoterId = @VoterId
            LIMIT 1;";

        var row = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { VoterId = voterId });
        if (row == null) return null;

        var baseDto = MapToSearchResult(row);
        return new VoterDetailDto
        {
            VoterId = baseDto.VoterId,
            RollVersionId = baseDto.RollVersionId,
            MaskedEPIC = baseDto.MaskedEPIC,
            OriginalName = baseDto.OriginalName,
            VerifiedEnglishName = baseDto.VerifiedEnglishName,
            RelativeName = baseDto.RelativeName,
            VerifiedEnglishRelativeName = baseDto.VerifiedEnglishRelativeName,
            Gender = baseDto.Gender,
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
            RollElectionName = (string)row.RollElectionName,
            RollVillageName = (string)row.RollVillageName,
            RollPublicationDate = (string?)row.RollPublicationDate
        };
    }

    private static VoterSearchResultDto MapToSearchResult(dynamic row)
    {
        string epic = (string)row.EPICNumber;
        return new VoterSearchResultDto
        {
            VoterId = (int)row.VoterId,
            RollVersionId = (int)row.RollVersionId,
            MaskedEPIC = MaskEPIC(epic),
            OriginalName = (string)row.OriginalName,
            VerifiedEnglishName = (string?)row.VerifiedEnglishName,
            RelativeName = (string?)row.RelativeName,
            VerifiedEnglishRelativeName = (string?)row.VerifiedEnglishRelativeName,
            Gender = (string?)row.Gender,
            WardNumber = (int)row.WardNumber,
            WardName = (string?)row.WardName,
            GramPanchayatSerialNumber = (int)row.GramPanchayatSerialNumber,
            AssemblyPartNumber = (int?)row.AssemblyPartNumber,
            AssemblySerialNumber = (int?)row.AssemblySerialNumber,
            HouseNumber = (string?)row.HouseNumber,
            SourcePageNumber = (int)row.SourcePageNumber,
            VerificationStatus = (string)row.VerificationStatus,
            PollingStationName = (string?)row.PollingStationName,
            PollingStationAddress = (string?)row.PollingStationAddress,
            PollingStationVerificationStatus = (string)row.PollingStationVerificationStatus
        };
    }
}
