namespace PaymentService.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using PaymentService.BLL.Interfaces;

[ApiController]
[Route("api/subscription-plans")]
public sealed class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _plans;
    public SubscriptionPlansController(ISubscriptionPlanService plans) => _plans = plans;

    [HttpGet("user")]
    public async Task<IActionResult> GetUserPlans(CancellationToken ct)
        => Ok(await _plans.GetUserPlansAsync(ct));

    [HttpGet("professional")]
    public async Task<IActionResult> GetProfessionalPlans(CancellationToken ct)
        => Ok(await _plans.GetProfessionalPlansAsync(ct));
}
