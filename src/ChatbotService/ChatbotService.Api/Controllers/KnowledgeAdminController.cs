using ChatbotService.BLL.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Chatbot;

namespace ChatbotService.Api.Controllers;

[ApiController]
[Route("api/chatbot/admin")]
public sealed class KnowledgeAdminController : ControllerBase
{
    private readonly IDocumentIngestionService _documentIngestionService;

    public KnowledgeAdminController(IDocumentIngestionService documentIngestionService)
    {
        _documentIngestionService = documentIngestionService;
    }

    [HttpPost("documents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> AddDocument([FromBody] IngestKnowledgeDocumentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Title and content are required.");
        }

        var documentId = await _documentIngestionService.IngestAsync(request, cancellationToken);
        return Ok(new { documentId });
    }

    [HttpPost("reindex")]
    [ProducesResponseType(typeof(ChatbotReindexResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatbotReindexResponse>> Reindex(CancellationToken cancellationToken)
        => Ok(new ChatbotReindexResponse { IndexedDocuments = await _documentIngestionService.ReindexAsync(cancellationToken) });
}
