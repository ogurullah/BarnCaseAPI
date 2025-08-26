using BarnCaseAPI.Services;
using BarnCaseAPI.Models;
using Microsoft.AspNetCore.Mvc;
using BarnCaseAPI.Security;
using Microsoft.AspNetCore.Authorization;

namespace BarnCaseAPI.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    public ProductsController(ProductService productService) => _productService = productService;

    public record SellRequest(int FarmId, int[] ProductIds);

    [Authorize]
    [HttpPost("sell")]
    public async Task<ActionResult<object>> SellAsync([FromBody] SellRequest request)
    {
        var userId = User.UserId();
        var total = await _productService.SellAsync(userId, request.FarmId, request.ProductIds);
        return new { total };
    }

    [HttpGet("view")]
    public async Task<ActionResult<IEnumerable<Product>>> ViewProducts([FromQuery] int farmId)
    {
        var products = await _productService.GetProductsByFarmAsync(farmId);
        return Ok(products);
    }
}
