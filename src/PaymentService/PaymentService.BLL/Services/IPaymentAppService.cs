using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentService.BLL.Services;

public interface IPaymentAppService
{
    Task<Guid> SimulateSuccessAsync(Guid userId, string planCode, decimal amount, CancellationToken ct);
}

