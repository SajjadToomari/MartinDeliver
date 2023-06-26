using WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DomainClasses;
using WebAPI.DataLayer.Context;
using WebAPI.Dto;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers;

[Route("api/[controller]/[action]"), Authorize(Policy = CustomRoles.B2B)]
public class B2BController : Controller
{
    private readonly ApplicationDbContext _context;

    public B2BController(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IActionResult> SubmitDeliveryRequest(DeliveryRequestDto deliveryRequestDto)
    {
        var delivery = deliveryRequestDto.Adapt<Delivery>();

        delivery.Status = Models.DeliveryStatus.WaitingForAccept;

        await _context.Deliveries.AddAsync(delivery);
        await _context.SaveChangesAsync();

        return Ok(delivery.Id);
    }

    public async Task<IActionResult> CancelDeliveryRequest(int deliveryId)
    {
        var userName = HttpContext.User?.Identity?.Name;

        var delivery = await _context.Deliveries
            .Where(x => x.Id == deliveryId && x.CreatedBy == userName && x.Status == Models.DeliveryStatus.WaitingForAccept)
            .FirstOrDefaultAsync();

        if (delivery == null)
        {
            return NotFound();
        }

        delivery.Status = Models.DeliveryStatus.Canceled;

        await _context.SaveChangesAsync();

        return Ok();
    }
}