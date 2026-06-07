namespace PaymentService.Api.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

public abstract class PaymentControllerBase : ControllerBase
{
    protected bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(claim, out userId)) return true;
        return Guid.TryParse(Request.Headers["X-User-Id"].ToString(), out userId);
    }

    protected bool TryGetProfessionalId(out Guid professionalId)
        => Guid.TryParse(Request.Headers["X-Professional-Id"].ToString(), out professionalId);

    protected bool IsAdmin()
    {
        if (User.IsInRole("Admin")) return true;
        return string.Equals(Request.Headers["X-User-Role"].ToString(), "Admin", StringComparison.OrdinalIgnoreCase);
    }
}
