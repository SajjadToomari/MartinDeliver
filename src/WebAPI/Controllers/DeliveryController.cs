using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Collections.Generic;
using WebAPI.DataLayer.Context;
using WebAPI.Models;
using WebAPI.Services;

namespace WebAPI.Controllers;

[Route("api/[controller]/[action]"), Authorize(Policy = CustomRoles.Delivery)]
public class DeliveryController : Controller
{
    static class Concurrency
    {
        public static ConcurrentDictionary<int, SemaphoreSlim> LockDelivery { get; set; } = new();
    }

    private readonly ApplicationDbContext _context;

    public DeliveryController(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    //TODO: Implement paging
    public async Task<IActionResult> GetInProgressDeliveries()
    {
        var deliveries = await _context.Deliveries.Where(x => x.Status == DeliveryStatus.InProgress).ToListAsync();

        return Ok(deliveries);
    }
    public async Task<IActionResult> AcceptDelivey(int deliveryId)
    {
        bool disposeNeeded = false;

        try
        {
            //Handle concurrency
            Concurrency.LockDelivery.TryAdd(deliveryId, new SemaphoreSlim(1, 1));
            Concurrency.LockDelivery.TryGetValue(deliveryId, out SemaphoreSlim semaphoreSlim);
            await semaphoreSlim.WaitAsync();

            disposeNeeded = true;

            var delivery = await _context.Deliveries.Where(x => x.Id == deliveryId).FirstOrDefaultAsync();
            if (delivery.Status != DeliveryStatus.WaitingForAccept)
            {
                return Ok(false);
            }

            delivery.Status = DeliveryStatus.InProgress;
            await _context.SaveChangesAsync();

            //TODO: Save user who accept delivery

            return Ok(true);
        }
        finally
        {
            if (disposeNeeded)
            {
                Concurrency.LockDelivery.TryGetValue(deliveryId, out SemaphoreSlim? semaphoreSlim);
                semaphoreSlim?.Release();
            }
        }
    }
}
