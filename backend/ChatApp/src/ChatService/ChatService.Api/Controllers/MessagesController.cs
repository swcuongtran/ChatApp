using ChatService.Api.DTOs;
using ChatService.Application.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using Utils.Correlation;

namespace ChatService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly SendMessageHandler _handler;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly ILogger<MessagesController> _logger;
        private readonly MarkAsReadHandler _markAsReadHandler;
        public MessagesController(SendMessageHandler handler, ILogger<MessagesController> logger, ICorrelationIdProvider correlationIdProvider, MarkAsReadHandler markAsReadHandler)
        {
            _handler = handler;
            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _markAsReadHandler = markAsReadHandler;
        }
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
        {
            var traceId = _correlationIdProvider.TraceId;
            var corrId = _correlationIdProvider.CorrelationId;
            var senderIdFromClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(senderIdFromClaim))
            {
                return Unauthorized("JWT Claim 'sub' not found.");
            }

            var command = new SendMessageCommand(
                ConversationId: request.ConversationId,
                SenderId: senderIdFromClaim,
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
        [HttpPost("mark-as-read")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request, CancellationToken cancellationToken)
        {
            var traceId = _correlationIdProvider.TraceId;
            var corrId = _correlationIdProvider.CorrelationId;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var command = new MarkAsReadCommand(
                ConversationId: request.ConversationId,
                UserId: userId,
                TraceId: traceId,
                CorrelationId: corrId
            );
            await _markAsReadHandler.Handle(command, cancellationToken);
            return Ok(new { message = "Marked as read successfully" });
        }
    }
}
