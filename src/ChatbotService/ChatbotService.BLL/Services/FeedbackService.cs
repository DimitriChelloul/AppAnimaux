using ChatbotService.BLL.Abstractions;
using ChatbotService.BLL.Security;
using ChatbotService.DAL.Abstractions;
using ChatbotService.Domain.Entities;
using Shared.Contracts.Chatbot;

namespace ChatbotService.BLL.Services;

public sealed class FeedbackService : IFeedbackService
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly InputSanitizer _sanitizer;

    public FeedbackService(IFeedbackRepository feedbackRepository, InputSanitizer sanitizer)
    {
        _feedbackRepository = feedbackRepository;
        _sanitizer = sanitizer;
    }

    public async Task<ChatbotFeedbackResponse> AddAsync(ChatbotFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Rating must be between 1 and 5.");
        }

        var id = await _feedbackRepository.AddAsync(new ChatFeedback
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            MessageId = request.MessageId,
            UserId = request.UserId,
            Rating = request.Rating,
            Comment = request.Comment is null ? null : _sanitizer.Sanitize(request.Comment, 2000)
        }, cancellationToken);

        return new ChatbotFeedbackResponse { FeedbackId = id };
    }
}
