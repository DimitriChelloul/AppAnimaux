namespace PaymentService.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using PaymentService.BLL.Services;

[ApiController]
[Route("payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentAppService _app;

    public PaymentsController(IPaymentAppService app) => _app = app;

    public sealed record SimulateRequest(Guid UserId, string PlanCode, decimal Amount);

    [HttpPost("simulate-success")]
    public async Task<IActionResult> SimulateSuccess([FromBody] SimulateRequest req, CancellationToken ct)
    {
        var id = await _app.SimulateSuccessAsync(req.UserId, req.PlanCode, req.Amount, ct);
        return Ok(new { paymentId = id });
    }
}

