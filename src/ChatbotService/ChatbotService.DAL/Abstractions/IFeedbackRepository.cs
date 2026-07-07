using ChatbotService.Domain.Entities;

namespace ChatbotService.DAL.Abstractions;

public interface IFeedbackRepository
{
    Task<Guid> AddAsync(ChatFeedback feedback, CancellationToken cancellationToken = default);
}
