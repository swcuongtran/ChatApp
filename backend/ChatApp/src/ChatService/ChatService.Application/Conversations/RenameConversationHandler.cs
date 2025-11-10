using BuildingBlock.CQRS;
using BuildingBlock.Exception;
using BuildingBlock.Outbox;
using ChatService.Application.Abstractions;
using MediatR;

namespace ChatService.Application.Conversations
{
    internal class RenameConversationHandler: ICommandHandler<RenameConversationCommand>
    {
        private readonly IConversationRepository _repo;
        private readonly IOutboxStore _outbox;
        public RenameConversationHandler(
            IConversationRepository repo,
            IOutboxStore outbox
            )
        {
            _repo = repo;
            _outbox = outbox;
        }

        public Task<Unit> Handle(RenameConversationCommand cmd, CancellationToken cancellationToken)
        {
            var Id = cmd.ConversationId;
            Guard.AgainstNullOrWhiteSpace(Id, nameof(cmd.ConversationId));
            var now = DateTimeOffset.UtcNow;
            var conv =  _repo.GetAsync(Id, cancellationToken).GetAwaiter().GetResult() 
                ?? throw new Exception($"Conversation with id {Id} not found.");
            if (conv.Id is null)
            {
                return Result.Fail(Error.NotFound("Conversation id is null"));
            }
            if (!conv.Members.Contains(cmd.ActorUserId))
            {
                throw new Exception("forbidden,User is not a member of the conversation.");
            }
        }
    }
}
