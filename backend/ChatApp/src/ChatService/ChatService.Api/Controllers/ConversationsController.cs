using ChatService.Api.DTOs;
using ChatService.Application.Conversations;
using ChatService.Application.Messages;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
        private readonly GetMessagesHandler _getMessagesHandler;

        public ConversationsController(
            ICorrelationIdProvider correlationProvider,
            CreateConversationHandler createHandler,
            RenameConversationHandler renameHandler,
            AddConversationMemberHandler addHandler,
            RemoveConversationMemberHandler removeHandler,
            GetConversationsHandler getConversationsHandler,
            GetMessagesHandler getMessagesHandler)
        {
            _correlationProvider = correlationProvider;
            _createHandler = createHandler;
            _renameHandler = renameHandler;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
            _getConversationsHandler = getConversationsHandler;
            _getMessagesHandler = getMessagesHandler;
        }

        [HttpPost]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request, CancellationToken cancellationToken)
        {
            // 1. FIX: Lấy ID người dùng từ Claim (Đã xác thực)
            var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(actorUserId))
            {
                return Unauthorized("Authentication required.");
            }

            var distinctMembers = new HashSet<string>(request.Members) { actorUserId };
            var membersList = distinctMembers.ToList();

            if (membersList[0] != actorUserId)
            {
                membersList.Remove(actorUserId);
                membersList.Insert(0, actorUserId);
            }

            if (request.IsDirect && membersList.Count != 2)
            {
                throw new InvalidOperationException("Direct conversation requires exactly 2 distinct members.");
            }

            var command = new CreateConversationCommand(
                request.IsDirect,
                membersList, 
                request.Title,
                request.IdempotencyKey,
                _correlationProvider.TraceId,
                _correlationProvider.CorrelationId
            );

            var result = await _createHandler.Handle(command, cancellationToken);
            return Ok(new { ConversationId = result });
        }

        [HttpPut("{id}/rename")]
        public async Task<IActionResult> RenameConversation(string id, [FromBody] RenameConversationRequest request, CancellationToken cancellationToken)
        {
            var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(actorUserId)) return Unauthorized("Authentication required.");

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
        public async Task<IActionResult> AddMembers(string id, [FromBody] UpdateMembersRequest request, CancellationToken cancellationToken)
        {
            var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(actorUserId)) return Unauthorized("Authentication required.");

            var command = new AddConversationMemberCommand(
                id,
                actorUserId, // SỬ DỤNG ID ĐÃ XÁC THỰC
                request.MemberUserIds,
                _correlationProvider.TraceId,
                _correlationProvider.CorrelationId
            );

            await _addHandler.Handle(command, cancellationToken);
            return Ok();
        }

        [HttpDelete("{id}/members")]
        public async Task<IActionResult> RemoveMembers(string id, [FromBody] UpdateMembersRequest request, CancellationToken cancellationToken)
        {
            var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(actorUserId)) return Unauthorized("Authentication required.");

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
        public async Task<IActionResult> GetList(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized("Authentication required.");

            var query = new GetConversationsQuery(userId);
            var result = await _getConversationsHandler.Handle(query, cancellationToken);
            return Ok(result);
        }
        [HttpGet("{id}/messages")]
        public async Task<IActionResult> GetMessages(string id, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized("Authentication required.");

            var query = new GetMessagesQuery(id, skip, take);
            var result = await _getMessagesHandler.Handle(query, cancellationToken);
            return Ok(result);
        }
    }
}
