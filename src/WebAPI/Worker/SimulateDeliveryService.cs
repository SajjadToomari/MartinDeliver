using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebAPI.DataLayer.Context;
using WebAPI.Models;

namespace WebAPI.Worker;

public class SimulateDeliveryService : BackgroundService
{
    private readonly ILogger<SimulateDeliveryService> _logger;
    private readonly IServiceScopeFactory _services;

    public SimulateDeliveryService(ILogger<SimulateDeliveryService> logger, IServiceScopeFactory services)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.UtcNow);

            await SimulateDeliveryMovement();

            await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
        }
    }

    private double GetRandomNumber()
    {
        var randomBytes = new byte[8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return BitConverter.ToDouble(randomBytes, 0);
    }

    private async Task SimulateDeliveryMovement()
    {
        using var scope = _services.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var inProgressDeliveries = await context.Deliveries.Where(x => x.Status == DeliveryStatus.InProgress).ToListAsync();

        foreach (var delivery in inProgressDeliveries)
        {
            // Simulate the movement by updating the delivery's current coordinates
            double newLatitude = delivery.CurrentLatitude + (GetRandomNumber() - 0.5) * 0.01;
            double newLongitude = delivery.CurrentLongitude + (GetRandomNumber() - 0.5) * 0.01;

            delivery.CurrentLatitude = newLatitude;
            delivery.CurrentLongitude = newLongitude;

            // Check if the delivery has reached the destination
            double distanceToDestination = CalculateDistance(newLatitude, newLongitude, delivery.DestinationLatitude, delivery.DestinationLongitude);
            if (distanceToDestination < 0.1) // Specify a suitable threshold for reaching the destination
            {
                // Update the delivery status to indicate completion
                delivery.Status = DeliveryStatus.Delivered;                
                _logger.LogInformation($"Delivery {delivery.Id} reached the destination.");

                await context.SaveChangesAsync();

                continue;
            }

            // Save the updated delivery
            await context.SaveChangesAsync();

            var companyWebHookAdress = delivery.User!.WebHookAddress;

            //TODO: Send location to web hook 

            _logger.LogInformation($"Delivery {delivery.Id} moved to Latitude: {newLatitude}, Longitude: {newLongitude}");
        }
    }
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Implementation of distance calculation using latitudes and longitudes
        // You can use various algorithms such as Haversine formula or Vincenty's formulae
        // Here's a simple calculation using Euclidean distance for demonstration purposes
        double dx = lon2 - lon1;
        double dy = lat2 - lat1;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
