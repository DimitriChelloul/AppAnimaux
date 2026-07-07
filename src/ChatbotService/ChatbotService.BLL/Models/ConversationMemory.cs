using ChatbotService.Domain.Entities;

namespace ChatbotService.BLL.Models;

public sealed record ConversationMemory(IReadOnlyList<ChatMessage> RecentMessages, string? Summary);
