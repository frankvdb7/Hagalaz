using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using MassTransit;
using Hagalaz.Exceptions;
using Hagalaz.Services.Contacts.Services;
using Hagalaz.Services.Contacts.Store;

namespace Hagalaz.Services.Contacts.Consumers
{
    public class AddRemoveContactConsumer : IConsumer<AddContactRequest>, IConsumer<RemoveContactRequest>
    {
        private readonly ContactSessionStore _contactSessions;
        private readonly WorldSessionStore _worldSessions;
        private readonly IContactService _contactService;
        private readonly ICharacterService _characterService;
        private readonly IMapper _mapper;

        public AddRemoveContactConsumer(ContactSessionStore contactSessions, WorldSessionStore worldSessions, IContactService contactService, ICharacterService characterService, IMapper mapper)
        {
            _contactSessions = contactSessions;
            _worldSessions = worldSessions;
            _contactService = contactService;
            _characterService = characterService;
            _mapper = mapper;
        }

        public async Task Consume(ConsumeContext<AddContactRequest> context)
        {
            var message = context.Message;
            var character = await _characterService.FindCharacterByIdAsync(message.MasterId);
            if (character == null)
            {
                throw new NotFoundException(nameof(character));
            }

            var characterContact = await _characterService.FindCharacterByDisplayName(message.ContactDisplayName);
            if (characterContact == null)
            {
                throw new NotFoundException(nameof(characterContact));
            }

            var result = await _contactService.AddContactAsync(message.MasterId, characterContact.MasterId, message.Ignore);
            if (result.IsNotFound)
            {
                throw new NotFoundException(nameof(character));
            }

            if (!result.Succeeded)
            {
                throw new NotImplementedException(nameof(result.Succeeded));
            }

            var contactFriend = await _contactService.FindFriendByIdAsync(characterContact.MasterId, message.MasterId);
            var contactSession = _contactSessions.GetOrDefault(characterContact.MasterId);
            var contactWorldSession = _worldSessions.GetOrDefault(contactSession?.WorldId ?? 0);
            var contactDto = _mapper.Map<ContactDto>(characterContact) with
            {
                AreMutualFriends = contactFriend != null,
                WorldId = contactWorldSession?.WorldId,
                WorldName = contactWorldSession?.WorldName,
                Rank = FriendsChatRank.Friend,
                Settings = new ContactSettingsDto(contactFriend?.Settings?.Availability.Off == true ? ContactAvailability.Off :
                    contactFriend?.Settings?.Availability.Friends == true ? ContactAvailability.Friends : ContactAvailability.Everyone)
            };
            var characterSession = _contactSessions.GetOrDefault(message.MasterId);
            var characterWorldSession = _worldSessions.GetOrDefault(characterSession?.WorldId ?? 0);
            var masterDto = _mapper.Map<ContactDto>(character) with
            { 
                WorldId = contactWorldSession?.WorldId,
                WorldName = characterWorldSession?.WorldName
            };
            var responseTask = context.RespondAsync(new AddContactResponse
            {
                MasterId = message.MasterId,
                Contact = contactDto
            });
            var publishTask = context.Publish(new ContactAddedMessage(masterDto, contactDto));
            await Task.WhenAll(responseTask, publishTask);
        }

        public async Task Consume(ConsumeContext<RemoveContactRequest> context)
        {
            var message = context.Message;
            var character = await _characterService.FindCharacterByIdAsync(message.MasterId);
            if (character == null)
            {
                throw new NotFoundException(nameof(character));
            }
            var characterContact = await _characterService.FindCharacterByDisplayName(message.ContactDisplayName);
            if (characterContact == null)
            {
                throw new NotFoundException(nameof(characterContact));
            }

            var result = await _contactService.RemoveContactAsync(message.MasterId, characterContact.MasterId);
            if (result.IsNotFound)
            {
                throw new NotFoundException(nameof(character));
            }

            if (!result.Succeeded)
            {
                throw new NotImplementedException(nameof(result.Succeeded));
            }

            var masterDto = _mapper.Map<ContactDto>(character);
            var contactDto = _mapper.Map<ContactDto>(characterContact);

            var respondTask = context.RespondAsync(new RemoveContactResponse
            {
                MasterId = message.MasterId, Contact = contactDto
            });
            
            var publishTask = context.Publish(new ContactRemovedMessage(masterDto, contactDto));
            await Task.WhenAll(respondTask, publishTask);
        }
    }
}