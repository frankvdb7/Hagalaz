using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using MassTransit;
using Hagalaz.Exceptions;
using Hagalaz.Services.Contacts.Services;
using Hagalaz.Services.Contacts.Store;
using ContactSettingsDto = Hagalaz.Services.Contacts.Services.Model.ContactSettingsDto;
using Model_ContactSettingsDto = Hagalaz.Services.Contacts.Services.Model.ContactSettingsDto;

namespace Hagalaz.Services.Contacts.Consumers
{
    public class SetContactSettingsConsumer : IConsumer<SetContactSettingsRequest>
    {
        private readonly IContactService _contactService;
        private readonly ICharacterService _characterService;
        private readonly ContactSessionStore _contactSessionStore;
        private readonly WorldSessionStore _worldSessionStore;
        private readonly IMapper _mapper;

        public SetContactSettingsConsumer(IContactService contactService, 
            ICharacterService characterService, 
            ContactSessionStore contactSessionStore, 
            WorldSessionStore worldSessionStore, 
            IMapper mapper)
        {
            _contactService = contactService;
            _characterService = characterService;
            _contactSessionStore = contactSessionStore;
            _worldSessionStore = worldSessionStore;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<SetContactSettingsRequest> context)
        {
            var message = context.Message;
            var settings = _mapper.Map<Model_ContactSettingsDto>(message.Settings);
            var result = await _contactService.SetContactSettingsAsync(message.MasterId, settings);
            if (result.IsNotFound)
            {
                throw new NotFoundException(nameof(message.MasterId));
            }
            if (!result.Succeeded)
            {
                throw new Exception("Something went wrong");
            }
            await context.RespondAsync(new SetContactSettingsResponse { });
            if (!_contactSessionStore.TryGetValue(message.MasterId, out var contactSession))
            {
                return;
            }
            if (!_worldSessionStore.TryGetValue(contactSession.WorldId, out var worldSession))
            {
                return;
            }

            var character = await _characterService.FindCharacterByIdAsync(message.MasterId);
            if (character == null)
            {
                return;
            }
            await context.Publish(new ContactSettingsChangedMessage(message.MasterId, new ContactDto
            {
                MasterId = message.MasterId,
                DisplayName = character.DisplayName,
                PreviousDisplayName = character.PreviousDisplayName,
                WorldId = worldSession.WorldId,
                WorldName = worldSession.WorldName,
                Settings = message.Settings
            }));
        }
    }
}
