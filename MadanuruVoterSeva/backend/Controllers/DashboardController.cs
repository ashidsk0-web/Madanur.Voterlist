using Microsoft.AspNetCore.Authorization;
using Dapper;
using MadanuruVoterSeva.Backend.DTOs;
using MadanuruVoterSeva.Backend.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MadanuruVoterSeva.Backend.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DashboardController(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<DashboardStatisticsDto>> GetStatistics()
    {
        using var connection = _connectionFactory.CreateConnection();

        // 1. Get active roll version
        var activeRoll = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
            SELECT RollVersionId, ElectionName, VillageName, DistrictName, CreatedDate, VerificationStatus
            FROM RollVersion 
            WHERE IsActive = 1 
            ORDER BY RollVersionId DESC 
            LIMIT 1;");

        int activeRollId = activeRoll != null ? (int)activeRoll.RollVersionId : 0;

        // 2. Count active voters & wards
        int totalVoters = 0;
        int totalWards = 0;
        int pendingVerification = 0;

        if (activeRollId > 0)
        {
            totalVoters = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Voter WHERE RollVersionId = @RollVersionId;",
                new { RollVersionId = activeRollId });

            totalWards = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM WardMaster WHERE RollVersionId = @RollVersionId;",
                new { RollVersionId = activeRollId });

            pendingVerification = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Voter WHERE RollVersionId = @RollVersionId AND VerificationStatus = 'Unverified';",
                new { RollVersionId = activeRollId });
        }

        // 3. Staging statistics
        int stagedCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM VoterImportStaging;");
        int validStaged = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM VoterImportStaging WHERE ValidationStatus = 'Valid';");
        int warningStaged = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM VoterImportStaging WHERE ValidationStatus = 'Warning';");
        int errorStaged = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM VoterImportStaging WHERE ValidationStatus = 'Error';");

        string validationStatus = "No Data";
        if (activeRoll != null && totalVoters > 0)
        {
            validationStatus = "Approved & Active";
        }
        else if (stagedCount > 0)
        {
            validationStatus = errorStaged > 0 ? "Staged (With Errors)" : (warningStaged > 0 ? "Staged (With Warnings)" : "Staged (Verified)");
        }

        var stats = new DashboardStatisticsDto
        {
            TotalImportedVoters = totalVoters,
            TotalWards = totalWards,
            ActiveRollVersion = activeRoll != null ? (string)activeRoll.ElectionName : "Not Activated",
            ActiveRollVersionId = activeRoll != null ? (int)activeRoll.RollVersionId : null,
            LastImportDate = activeRoll != null ? (string)activeRoll.CreatedDate : null,
            RecordsPendingVerification = pendingVerification,
            ImportValidationStatus = validationStatus,
            StagedRecordsCount = stagedCount,
            ValidStagedRecordsCount = validStaged,
            WarningStagedRecordsCount = warningStaged,
            ErrorStagedRecordsCount = errorStaged
        };

        return Ok(stats);
    }
}
