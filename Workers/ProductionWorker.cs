using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BarnCaseAPI.Services;
using BarnCaseAPI.Data;  // adjust if your DbContext is in a different namespace
using Microsoft.EntityFrameworkCore;

namespace BarnCaseAPI.Workers;

public sealed class ProductionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductionWorker> _logger;

    public ProductionWorker(IServiceScopeFactory scopeFactory, ILogger<ProductionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var production = scope.ServiceProvider.GetRequiredService<ProductionService>();

                var farmIds = await db.Farms
                    .AsNoTracking()
                    .Select(f => f.Id)
                    .ToListAsync(stoppingToken);

                var now = DateTime.UtcNow;

                foreach (var farmId in farmIds)
                {
                    await production.TickAsync(farmId, now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during production tick.");
            }

            // wait 30 seconds before running again
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
