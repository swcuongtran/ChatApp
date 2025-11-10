using ChatService.Api.DTOs;
using ChatService.Application.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly SendMessageHandler _handler;
        public MessagesController(SendMessageHandler handler)
        {
            _handler = handler;
        }
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
        {
            var traceId = HttpContext.TraceIdentifier;
            var corrId = HttpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                     ?? Guid.NewGuid().ToString("N");

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
