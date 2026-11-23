using Dapper;
using MadanuruVoterSeva.Backend.Interfaces;
using MadanuruVoterSeva.Backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace MadanuruVoterSeva.Backend.Controllers;

[ApiController]
[Route("api/roll-versions")]
public class RollVersionsController : ControllerBase
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RollVersionsController(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    [HttpGet("active")]
    public async Task<ActionResult<RollVersion>> GetActive()
    {
        using var connection = _connectionFactory.CreateConnection();

        var roll = await connection.QuerySingleOrDefaultAsync<RollVersion>(@"
            SELECT * FROM RollVersion 
            WHERE IsActive = 1 
            ORDER BY RollVersionId DESC 
            LIMIT 1;");

        if (roll == null)
        {
            return NotFound(new { message = "No active electoral roll version found." });
        }

        return Ok(roll);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RollVersion>>> GetAll()
    {
        using var connection = _connectionFactory.CreateConnection();
        var rolls = await connection.QueryAsync<RollVersion>(
            "SELECT * FROM RollVersion ORDER BY RollVersionId DESC;");
        return Ok(rolls);
    }
}
