using ChatbotService.BLL.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Contracts.Chatbot;

namespace ChatbotService.Api.Controllers;

[ApiController]
[EnableRateLimiting("chatbot")]
[Route("api/chatbot")]
public sealed class ChatbotController : ControllerBase
{
    private readonly IChatbotOrchestrator _orchestrator;
    private readonly IDocumentIngestionService _documentIngestionService;
    private readonly IDocumentQueryService _documentQueryService;
    private readonly IConversationQueryService _conversationQueryService;
    private readonly IFeedbackService _feedbackService;

    public ChatbotController(
        IChatbotOrchestrator orchestrator,
        IDocumentIngestionService documentIngestionService,
        IDocumentQueryService documentQueryService,
        IConversationQueryService conversationQueryService,
        IFeedbackService feedbackService)
    {
        _orchestrator = orchestrator;
        _documentIngestionService = documentIngestionService;
        _documentQueryService = documentQueryService;
        _conversationQueryService = conversationQueryService;
        _feedbackService = feedbackService;
    }

    [HttpPost("ask")]
    [ProducesResponseType(typeof(AskChatbotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AskChatbotResponse>> Ask([FromBody] AskChatbotRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required.");
        }

        return Ok(await _orchestrator.AskAsync(request, cancellationToken));
    }

    [HttpPost("document")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> UploadDocument([FromBody] UploadKnowledgeDocumentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Title and content are required.");
        }

        var documentId = await _documentIngestionService.IngestAsync(new IngestKnowledgeDocumentRequest
        {
            Title = request.Title,
            Content = request.Content,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SourceType = request.SourceType,
            SourceUri = request.SourceUri,
            Locale = request.Locale
        }, cancellationToken);

        return Ok(new { documentId });
    }

    [HttpPost("reindex")]
    [ProducesResponseType(typeof(ChatbotReindexResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatbotReindexResponse>> Reindex(CancellationToken cancellationToken)
        => Ok(new ChatbotReindexResponse { IndexedDocuments = await _documentIngestionService.ReindexAsync(cancellationToken) });

    [HttpDelete("document/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
    {
        await _documentQueryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("documents")]
    [ProducesResponseType(typeof(IReadOnlyList<ChatbotDocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ChatbotDocumentDto>>> Documents(CancellationToken cancellationToken)
        => Ok(await _documentQueryService.ListAsync(cancellationToken));

    [HttpGet("documents/statistics")]
    [ProducesResponseType(typeof(ChatbotStatisticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatbotStatisticsDto>> Statistics(CancellationToken cancellationToken)
        => Ok(await _documentQueryService.GetStatisticsAsync(cancellationToken));

    [HttpGet("conversations/{id:guid}")]
    [ProducesResponseType(typeof(ChatbotConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatbotConversationDto>> Conversation(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _conversationQueryService.GetAsync(id, cancellationToken);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    [HttpPost("feedback")]
    [ProducesResponseType(typeof(ChatbotFeedbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatbotFeedbackResponse>> Feedback([FromBody] ChatbotFeedbackRequest request, CancellationToken cancellationToken)
    {
        if (request.ConversationId == Guid.Empty)
        {
            return BadRequest("ConversationId is required.");
        }

        return Ok(await _feedbackService.AddAsync(request, cancellationToken));
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(ChatbotHealthResponse), StatusCodes.Status200OK)]
    public ActionResult<ChatbotHealthResponse> Health()
        => Ok(new ChatbotHealthResponse { Status = "ok", CheckedAt = DateTimeOffset.UtcNow });
}
