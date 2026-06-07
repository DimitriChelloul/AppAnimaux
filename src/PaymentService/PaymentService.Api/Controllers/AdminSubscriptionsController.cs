namespace PaymentService.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using PaymentService.BLL.Interfaces;
using PaymentService.DAL.Interfaces;

[ApiController]
[Route("api/admin/subscriptions")]
public sealed class AdminSubscriptionsController : PaymentControllerBase
{
    private readonly IUserSubscriptionRepository _users;
    private readonly IProfessionalSubscriptionRepository _professionals;

    public AdminSubscriptionsController(IUserSubscriptionRepository users, IProfessionalSubscriptionRepository professionals)
    {
        _users = users;
        _professionals = professionals;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => IsAdmin() ? Ok(await _users.ListAsync(page, pageSize, ct)) : Forbid();

    [HttpGet("professionals")]
    public async Task<IActionResult> GetProfessionals([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => IsAdmin() ? Ok(await _professionals.ListAsync(page, pageSize, ct)) : Forbid();

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
        => IsAdmin() ? Ok(new { id, message = "Use users/professionals listing for details." }) : Forbid();

    [HttpPost("{id:guid}/force-cancel")]
    public IActionResult ForceCancel(Guid id)
        => IsAdmin() ? Accepted(new { id, message = "Force cancel command accepted." }) : Forbid();

    [HttpPost("{id:guid}/sync")]
    public IActionResult Sync(Guid id)
        => IsAdmin() ? Accepted(new { id, message = "Subscription sync command accepted." }) : Forbid();
}
