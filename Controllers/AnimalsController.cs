using BarnCaseAPI.Services;
using BarnCaseAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace BarnCaseAPI.Controllers;

[ApiController]
[Route("api/animals")]
public class AnimalsController : ControllerBase
{
    private readonly AnimalService _animals;
    public AnimalsController(AnimalService animals) => _animals = animals;

    [HttpPost("buy")]
    public async Task<ActionResult<Animal>> Buy([FromQuery] int userId, [FromQuery] int farmId, [FromBody] BuyAnimalRequest request)
        => await _animals.BuyAsync(userId, farmId, request.Species);

    [HttpPost("{animalId:int}/sell")]
    public async Task<ActionResult<object>> Sell([FromRoute] int animalId, [FromQuery] int userId)
    {
        var earned = await _animals.SellAsync(userId, animalId);
        return new { earned };
    }

}

public record BuyAnimalRequest(AnimalSpecies Species);