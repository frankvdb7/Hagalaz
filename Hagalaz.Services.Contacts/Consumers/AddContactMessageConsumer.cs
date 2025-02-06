using AutoMapper;
using Hagalaz.Contacts.Messages;
using MassTransit;
using Hagalaz.Exceptions;
using Hagalaz.Services.Contacts.Services;
using Hagalaz.Services.Contacts.Store;

namespace Hagalaz.Services.Contacts.Consumers
{
    public class AddContactMessageConsumer : IConsumer<AddContactMessageRequest>
    {
        private readonly ICharacterService _characterService;
        private readonly IContactService _contactService;
        private readonly ContactSessionStore _contactSessionStore;
        private readonly WorldSessionStore _worldSessionStore;
        private readonly IMapper _mapper;

        public AddContactMessageConsumer(
            ICharacterService characterService, IContactService contactService, ContactSessionStore contactSessionStore, WorldSessionStore worldSessionStore,
            IMapper mapper)
        {
            _characterService = characterService;
            _contactService = contactService;
            _contactSessionStore = contactSessionStore;
            _worldSessionStore = worldSessionStore;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<AddContactMessageRequest> context)
        {
            var message = context.Message;
            var receiver = await _characterService.FindCharacterByDisplayName(message.ContactDisplayName);
            if (receiver == null || !_contactSessionStore.TryGetValue(receiver.MasterId, out var session) ||
                !_worldSessionStore.TryGetValue(session.WorldId, out var world))
            {
                throw new NotFoundException(nameof(receiver));
            }

            var ignore = await _contactService.FindIgnoreByIdAsync(receiver.MasterId, message.MasterId);
            if (ignore != null)
            {
                throw new NotAllowedException(nameof(ignore));
            }

            var sender = await _characterService.FindCharacterByIdAsync(message.MasterId);
            if (sender == null)
            {
                throw new NotFoundException(nameof(sender));
            }

            var senderDto = _mapper.Map<ContactMessageNotification.SenderDto>(sender);
            await context.Publish(new ContactMessageNotification()
                {
                    MasterId = receiver.MasterId, MessageLength = message.MessageLength, MessagePayload = message.MessagePayload, Sender = senderDto
                });

            await context.RespondAsync(new AddContactMessageResponse());
        }
    }
}