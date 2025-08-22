using BarnCaseAPI.Models;
using BarnCaseAPI.Services;
using BarnCaseAPI.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Farm>> Create([FromBody] CreateFarmRequest request)
    {
        var userId = User.UserId();
        var farm = await _farms.CreateFarmAsync(userId, request.farmName);
        return Ok(farm);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Farm?>> Get(int id) => await _farms.GetFarmAsync(id);

//    [HttpPost("{id:int}/tick")]
//    public async Task<ActionResult<object>> Tick(int id)
//    {
//        var created = await _production.TickAsync(id);
//        return new { created };
//    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<Farm>>> GetMine()
    {
        var userId = User.UserId();
        var farms = await _farms.GetAllFarmsForUserAsync(userId);
        return Ok(farms);
    }
}
public record CreateFarmRequest(string farmName);

