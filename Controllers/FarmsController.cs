using BarnCaseAPI.Models;
using BarnCaseAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarnCaseAPI.Controllers;

[ApiController]
[Route("api/farms")]

public class FarmsController : ControllerBase
{
    private readonly FarmService _farms;
    private readonly ProductionService _production;

    public FarmsController(FarmService farms, ProductionService production)
    {
        _farms = farms;
        _production = production;
    }

    [HttpPost]
    public async Task<ActionResult<Farm>> Create([FromQuery] int ownerId, [FromBody] CreateFarmRequest request)
        => await _farms.CreateFarmAsync(ownerId, request.Name);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Farm?>> Get(int id) => await _farms.GetFarmAsync(id);

    [HttpPost("{id:int}/tick")]
    public async Task<ActionResult<object>> Tick(int id)
    {
        var created = await _production.TickAsync(id);
        return new { created };
    }

}

public record CreateFarmRequest(string Name);

