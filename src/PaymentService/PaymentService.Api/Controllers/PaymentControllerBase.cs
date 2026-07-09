using Microsoft.AspNetCore.Mvc;
using Shared.Security;

namespace PaymentService.Api.Controllers;

public abstract class PaymentControllerBase : ControllerBase
{
    protected bool TryGetUserId(out Guid userId)
    {
        var claim = User.GetUserId();
        if (claim.HasValue)
        {
            userId = claim.Value;
            return true;
        }

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