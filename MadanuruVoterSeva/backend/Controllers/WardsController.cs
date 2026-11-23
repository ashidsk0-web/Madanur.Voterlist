using Microsoft.AspNetCore.Authorization;
using Dapper;
using MadanuruVoterSeva.Backend.DTOs;
using MadanuruVoterSeva.Backend.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MadanuruVoterSeva.Backend.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class WardsController : ControllerBase
{
    private readonly IDbConnectionFactory _connectionFactory;

    public WardsController(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WardSummaryDto>>> GetWards()
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT 
                w.WardId,
                w.WardNumber,
                w.WardName,
                COUNT(v.VoterId) AS TotalVoters,
                SUM(CASE WHEN v.VerificationStatus = 'Verified' THEN 1 ELSE 0 END) AS VerifiedVoters,
                COALESCE(MIN(v.GramPanchayatSerialNumber), 0) AS MinSerialNumber,
                COALESCE(MAX(v.GramPanchayatSerialNumber), 0) AS MaxSerialNumber
            FROM WardMaster w
            JOIN RollVersion rv ON w.RollVersionId = rv.RollVersionId
            LEFT JOIN Voter v ON w.WardId = v.WardId
            WHERE rv.IsActive = 1
            GROUP BY w.WardId, w.WardNumber, w.WardName
            ORDER BY w.WardNumber ASC;";

        var list = await connection.QueryAsync<WardSummaryDto>(sql);
        return Ok(list);
    }

    [HttpGet("{wardId}/voters")]
    public async Task<ActionResult<PagedResult<VoterSearchResultDto>>> GetVotersByWard(
        int wardId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        long offset = (page - 1L) * pageSize;

        using var connection = _connectionFactory.CreateConnection();

        int totalCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Voter WHERE WardId = @WardId AND RollVersionId = (SELECT RollVersionId FROM RollVersion WHERE IsActive = 1);",
            new { WardId = wardId });

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
            LEFT JOIN VoterPollingStation vps ON vps.Id = (
                SELECT MIN(a.Id) FROM VoterPollingStation a
                JOIN PollingStation station ON station.PollingStationId = a.PollingStationId
                WHERE a.VoterId = v.VoterId AND a.VerificationStatus = 'Verified'
                  AND station.RollVersionId = v.RollVersionId AND station.VerificationStatus = 'Verified'
                HAVING COUNT(*) = 1
            )
            LEFT JOIN PollingStation ps ON vps.PollingStationId = ps.PollingStationId
            WHERE v.WardId = @WardId AND v.RollVersionId = (SELECT RollVersionId FROM RollVersion WHERE IsActive = 1)
            ORDER BY v.GramPanchayatSerialNumber ASC
            LIMIT @PageSize OFFSET @Offset;";

        var rows = await connection.QueryAsync<dynamic>(sql, new { WardId = wardId, PageSize = pageSize, Offset = offset });

        var items = rows.Select(row => Services.VoterSearchService.MapToSearchResult(row)).Cast<VoterSearchResultDto>().ToList();

        return Ok(new PagedResult<VoterSearchResultDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }
}
