using BuildingBlock.CQRS;
using BuildingBlock.Outbox;
using ChatService.Application.Abstractions;
using MediatR;

namespace ChatService.Application.Conversations
{
    internal class RenameConversationHandler : ICommandHandler<RenameConversationCommand>
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

        public async Task<Unit> Handle(RenameConversationCommand cmd, CancellationToken cancellationToken)
        {
            
        }
    }
}
