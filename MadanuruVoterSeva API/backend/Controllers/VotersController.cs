using Microsoft.AspNetCore.Authorization;
using MadanuruVoterSeva.Backend.DTOs;
using MadanuruVoterSeva.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace MadanuruVoterSeva.Backend.Controllers;

[ApiController]
[Authorize(Roles = "Admin,DeskUser")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/[controller]")]
public class VotersController : ControllerBase
{
    private readonly VoterSearchService _searchService;
    private readonly PdfExtractionService _pdfService;

    public VotersController(VoterSearchService searchService, PdfExtractionService pdfService)
    {
        _searchService = searchService;
        _pdfService = pdfService;
    }

    [HttpGet("search/epic")]
    public async Task<ActionResult<VoterSearchResultDto>> SearchByEpic([FromQuery] string epicNumber)
    {
        if (string.IsNullOrWhiteSpace(epicNumber) || epicNumber.Length > 40)
        {
            return BadRequest(new { message = "Enter a valid EPIC number." });
        }

        var result = await _searchService.SearchByEpicAsync(epicNumber);
        if (result == null)
        {
            return NotFound(new { message = "No matching EPIC was found in the local roll." });
        }

        return Ok(result);
    }

    [HttpGet("search/name")]
    public async Task<ActionResult<PagedResult<VoterSearchResultDto>>> SearchByName(
        [FromQuery] string? name,
        [FromQuery] int? wardNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15)
    {
        if (name is { Length: > 100 } || (string.IsNullOrWhiteSpace(name) && wardNumber is null or <= 0))
            return BadRequest(new { message = "Enter a name or select a ward." });

        var result = await _searchService.SearchByNameAsync(name, wardNumber, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VoterDetailDto>> GetById(int id)
    {
        var result = await _searchService.GetVoterByIdAsync(id);
        if (result == null)
        {
            return NotFound(new { message = $"Voter with ID {id} not found." });
        }

        return Ok(result);
    }

    [HttpGet("{id}/source/pdf")]
    public async Task<IActionResult> GetRollPdf(int id)
    {
        var voter = await _searchService.GetVoterByIdAsync(id);
        var path = _pdfService.GetPdfPath();
        return voter is null || !System.IO.File.Exists(path)
            ? NotFound()
            : PhysicalFile(path, "application/pdf", enableRangeProcessing: true);
    }

    [HttpGet("{id}/source")]
    public async Task<ActionResult> GetVoterSource(int id)
    {
        var voter = await _searchService.GetVoterByIdAsync(id);
        if (voter == null)
        {
            return NotFound(new { message = $"Voter with ID {id} not found." });
        }

        var pdfPath = _pdfService.GetPdfPath();
        bool pdfExists = System.IO.File.Exists(pdfPath);

        return Ok(new
        {
            voterId = voter.VoterId,
            sourcePageNumber = voter.SourcePageNumber,
            gramPanchayatSerialNumber = voter.GramPanchayatSerialNumber,
            wardNumber = voter.WardNumber,
            originalPdfAvailable = pdfExists,
            originalPdfPath = pdfExists ? Url.Action(nameof(GetRollPdf), new { id }) : null,
            message = pdfExists 
                ? $"Voter record is on page {voter.SourcePageNumber} of {System.IO.Path.GetFileName(pdfPath)}"
                : "Source PDF file is not available locally."
        });
    }

    [HttpGet("{id}/photo")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVoterPhoto(int id)
    {
        var photosDir = Path.GetFullPath("../data/photos");
        var idPath = Path.Combine(photosDir, $"{id}.jpg");
        if (System.IO.File.Exists(idPath))
        {
            return PhysicalFile(idPath, "image/jpeg");
        }

        var voter = await _searchService.GetVoterByIdAsync(id);
        if (voter != null)
        {
            var epic = System.Text.RegularExpressions.Regex.Replace(voter.MaskedEPIC, @"[^A-Za-z0-9]", "");
            var epicPath = Path.Combine(photosDir, $"{epic}.jpg");
            if (System.IO.File.Exists(epicPath))
            {
                return PhysicalFile(epicPath, "image/jpeg");
            }
        }

        return NotFound();
    }

    [HttpGet("epic/{epic}/photo")]
    [AllowAnonymous]
    public IActionResult GetVoterPhotoByEpic(string epic)
    {
        var clean = System.Text.RegularExpressions.Regex.Replace(epic ?? "", @"[^A-Za-z0-9]", "").ToUpperInvariant();
        var photosDir = Path.GetFullPath("../data/photos");
        var epicPath = Path.Combine(photosDir, $"{clean}.jpg");
        if (System.IO.File.Exists(epicPath))
        {
            return PhysicalFile(epicPath, "image/jpeg");
        }
        return NotFound();
    }
}
