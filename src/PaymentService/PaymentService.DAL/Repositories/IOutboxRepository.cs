using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentService.DAL.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(Guid messageId, string type, string payloadJson, string? aggregateType, Guid? aggregateId, CancellationToken ct);
}

