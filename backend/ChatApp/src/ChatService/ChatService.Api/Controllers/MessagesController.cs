using ChatService.Api.DTOs;
using ChatService.Application.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
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
        private readonly GetMessagesHandler _getMessagesHandler;
        public MessagesController(SendMessageHandler handler, ILogger<MessagesController> logger, ICorrelationIdProvider correlationIdProvider, GetMessagesHandler getMessagesHandler)
        {
            _handler = handler;
            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _getMessagesHandler = getMessagesHandler;
        }
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
        {
            var traceId = _correlationIdProvider.TraceId;
            var corrId = _correlationIdProvider.CorrelationId;

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

        [HttpGet("{id}/messages")]
        public async Task<IActionResult> GetMessages(string id, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
        {
            var query = new GetMessagesQuery(id, skip, take);
            var result = await _getMessagesHandler.Handle(query, cancellationToken);
            return Ok(result);
        }
    }
}
