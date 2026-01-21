using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentService.DAL.Repositories;

using PaymentService.Domain.Entities;

public interface IPaymentRepository
{
    Task<Guid> InsertAsync(Payment p, CancellationToken ct);
}

