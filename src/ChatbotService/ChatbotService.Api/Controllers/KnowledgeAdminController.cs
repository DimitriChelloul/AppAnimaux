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
    public async Task<ActionResult<object>> Reindex(CancellationToken cancellationToken)
    {
        var indexedDocuments = await _documentIngestionService.ReindexAsync(cancellationToken);
        return Ok(new { indexedDocuments });
    }
}
