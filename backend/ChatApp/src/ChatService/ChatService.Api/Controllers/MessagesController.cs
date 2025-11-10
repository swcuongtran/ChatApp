using ChatService.Api.DTOs;
using ChatService.Application.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ChatService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly SendMessageHandler _handler;
        private readonly ILogger<MessagesController> _logger;
        public MessagesController(SendMessageHandler handler, ILogger<MessagesController> logger)
        {
            _handler = handler;
            _logger = logger;
        }
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
        {
            var traceId = Activity.Current?.TraceId.ToString();
            var corrId = HttpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(corrId))
            {
                corrId = Guid.NewGuid().ToString("N");
                _logger.LogWarning("X-Correlation-Id was missing, generated new one: {CorrelationId}", corrId);
            }

            var command = new SendMessageCommand(
                ConversationId: request.ConversationId,
                SenderId: request.SenderId,
                Content: request.Content,
                MessageId: request.MessageId
            );

            var result = await _handler.Handle(command with { TraceId = traceId, CorrelationId = corrId }, cancellationToken);
            if (result is not null)
            {
                return Ok(new { MessageId = result, createdAtUtc = DateTimeOffset.UtcNow });
            }
            return BadRequest("Failed to send message.");
        }
    }
}
