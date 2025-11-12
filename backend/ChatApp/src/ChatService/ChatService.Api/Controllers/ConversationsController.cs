using ChatService.Api.DTOs;
using ChatService.Application.Conversations;
using Microsoft.AspNetCore.Mvc;
using Utils.Correlation;

namespace ChatService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly ICorrelationIdProvider _correlationProvider;
        private readonly CreateConversationHandler _createHandler;
        private readonly RenameConversationHandler _renameHandler;
        private readonly AddConversationMemberHandler _addHandler;
        private readonly RemoveConversationMemberHandler _removeHandler;
        private readonly GetConversationsHandler _getConversationsHandler;

        public ConversationsController(
            ICorrelationIdProvider correlationProvider,
            CreateConversationHandler createHandler,
            RenameConversationHandler renameHandler,
            AddConversationMemberHandler addHandler,
            RemoveConversationMemberHandler removeHandler,
            GetConversationsHandler getConversationsHandler)
        {
            _correlationProvider = correlationProvider;
            _createHandler = createHandler;
            _renameHandler = renameHandler;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
            _getConversationsHandler = getConversationsHandler;
        }

        [HttpPost]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request, CancellationToken cancellationToken)
        {
            var command = new CreateConversationCommand(
                request.IsDirect,
                request.Members,
                request.Title,
                request.IdempotencyKey,
                _correlationProvider.TraceId,
                _correlationProvider.CorrelationId
            );

            var result = await _createHandler.Handle(command, cancellationToken);
            return Ok(new { ConversationId = result });
        }

        [HttpPut("{id}/rename")]
        public async Task<IActionResult> RenameConversation(string id, [FromBody] RenameConversationRequest request, [FromHeader(Name = "X-User-Id")] string actorUserId, CancellationToken cancellationToken)
        {
            // TODO: Lấy actorUserId từ JWT Claim thay vì Header
            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                return Unauthorized("X-User-Id header is required.");
            }

            var command = new RenameConversationCommand(
                id,
                actorUserId,
                request.NewTitle,
                _correlationProvider.TraceId,
                _correlationProvider.CorrelationId
            );

            await _renameHandler.Handle(command, cancellationToken);
            return Ok();
        }

        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMembers(string id, [FromBody] UpdateMembersRequest request, [FromHeader(Name = "X-User-Id")] string actorUserId, CancellationToken cancellationToken)
        {
            // TODO: Lấy actorUserId từ JWT Claim thay vì Header
            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                return Unauthorized("X-User-Id header is required.");
            }

            var command = new AddConversationMemberCommand(
                id,
                actorUserId,
                request.MemberUserIds,
                _correlationProvider.TraceId,
                _correlationProvider.CorrelationId
            );

            await _addHandler.Handle(command, cancellationToken);
            return Ok();
        }

        [HttpDelete("{id}/members")]
        public async Task<IActionResult> RemoveMembers(string id, [FromBody] UpdateMembersRequest request, [FromHeader(Name = "X-User-Id")] string actorUserId, CancellationToken cancellationToken)
        {
            // TODO: Lấy actorUserId từ JWT Claim thay vì Header
            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                return Unauthorized("X-User-Id header is required.");
            }

            var command = new RemoveConversationMemberCommand(
                id,
                actorUserId,
                request.MemberUserIds,
                _correlationProvider.TraceId,
                _correlationProvider.CorrelationId
            );

            await _removeHandler.Handle(command, cancellationToken);
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> GetList([FromHeader(Name = "X-User-Id")] string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized("X-User-Id header is required.");
            }

            var query = new GetConversationsQuery(userId);
            var result = await _getConversationsHandler.Handle(query, cancellationToken);
            return Ok(result);
        }
    }
}
