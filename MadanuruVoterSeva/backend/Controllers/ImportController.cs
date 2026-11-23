using Microsoft.AspNetCore.Authorization;
using Dapper;
using MadanuruVoterSeva.Backend.DTOs;
using MadanuruVoterSeva.Backend.Interfaces;
using MadanuruVoterSeva.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace MadanuruVoterSeva.Backend.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly PdfExtractionService _pdfService;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ImportController> _logger;

    public ImportController(
        PdfExtractionService pdfService, 
        IDbConnectionFactory connectionFactory,
        ILogger<ImportController> logger)
    {
        _pdfService = pdfService;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    [HttpGet("preview")]
    [HttpPost("preview")]
    public ActionResult<ImportPreviewDto> Preview()
    {
        var preview = _pdfService.PreviewPdf();
        return Ok(preview);
    }

    [HttpPost("extract")]
    public async Task<ActionResult> Extract()
    {
        try
        {
            _logger.LogInformation("Import extract request received.");
            int rollId = await _pdfService.ExtractAndStageRollAsync();
            var summary = await _pdfService.ValidateStagingAsync(rollId);
            return Ok(new
            {
                message = "PDF extracted and staged successfully.",
                rollVersionId = rollId,
                validation = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extraction failed.");
            return StatusCode(500, new { message = "The roll could not be extracted. Check the local API logs." });
        }
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ImportValidationSummaryDto>> Validate([FromQuery] int? rollVersionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        int targetId = rollVersionId ?? await connection.ExecuteScalarAsync<int>(
            "SELECT COALESCE(MAX(RollVersionId), 0) FROM RollVersion;");

        if (targetId == 0)
        {
            return BadRequest(new { message = "No roll version available to validate." });
        }

        var summary = await _pdfService.ValidateStagingAsync(targetId);
        return Ok(summary);
    }

    [HttpPost("approve")]
    public async Task<ActionResult> Approve([FromQuery] int? rollVersionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        int targetId = rollVersionId ?? await connection.ExecuteScalarAsync<int>(
            "SELECT COALESCE(MAX(RollVersionId), 0) FROM RollVersion;");

        if (targetId == 0)
        {
            return BadRequest(new { message = "No roll version available to approve." });
        }

        bool success = await _pdfService.ApproveRollVersionAsync(targetId);
        if (!success) return Conflict(new { message = "This staged version must pass validation before activation." });
        return Ok(new { message = "Electoral roll approved and activated successfully.", rollVersionId = targetId, success });
    }

    [HttpGet("status")]
    public async Task<ActionResult> GetStatus()
    {
        using var connection = _connectionFactory.CreateConnection();

        var latestRoll = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM RollVersion ORDER BY RollVersionId DESC LIMIT 1;");

        int stagedCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM VoterImportStaging;");

        int voterCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Voter WHERE RollVersionId = (SELECT RollVersionId FROM RollVersion WHERE IsActive = 1);");

        return Ok(new
        {
            latestRoll,
            stagedCount,
            activeVotersCount = voterCount,
            status = latestRoll?.VerificationStatus ?? "NoRoll"
        });
    }
}
