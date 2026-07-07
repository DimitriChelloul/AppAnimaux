using ChatbotService.BLL.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Chatbot;

namespace ChatbotService.Api.Controllers;

[ApiController]
[Route("api/chatbot")]
public sealed class ChatbotController : ControllerBase
{
    private readonly IChatbotOrchestrator _orchestrator;

    public ChatbotController(IChatbotOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<AskChatbotResponse>> Ask([FromBody] AskChatbotRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required.");
        }

        var response = await _orchestrator.AskAsync(request, cancellationToken);
        return Ok(response);
    }
}
