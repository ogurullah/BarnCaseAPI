using BarnCaseAPI.Services;
using BarnCaseAPI.Models;
using Microsoft.AspNetCore.Mvc;
using BarnCaseAPI.Security;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BarnCaseAPI.Controllers;

[ApiController]
[Route("api/animals")]
public class AnimalsController : ControllerBase
{
    private readonly AnimalService _animals;
    public AnimalsController(AnimalService animals) => _animals = animals;

    [Authorize]
    [HttpPost("buy")]
    public async Task<ActionResult<Animal>> Buy([FromQuery] int farmId, [FromBody] BuyAnimalRequest request)
    {
        var userId = User.UserId();
        return await _animals.BuyAsync(userId, farmId, request.Species);
    }

    [Authorize]
    [HttpPost("{animalId:int}/sell")]
    public async Task<ActionResult<object>> Sell([FromRoute] int animalId)
    {
        var userId = User.UserId();
        var earned = await _animals.SellAsync(userId, animalId);
        return new { earned };
    }

    [HttpGet("{farmId:int}")]
    public async Task<ActionResult<Animal?>> Get([FromRoute] int farmId)
    {
        var animals = await _animals.ViewAnimalsAsync(farmId);
        return Ok(animals);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<object>>> GetMine()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId)) return Ok(Array.Empty<object>());

        var dict = await _animals.GetAnimalCountsForUserAsync(userId);
        return Ok(dict.Select(kv => new { kind = kv.Key, count = kv.Value }));
    }
}

public record BuyAnimalRequest(AnimalSpecies Species);