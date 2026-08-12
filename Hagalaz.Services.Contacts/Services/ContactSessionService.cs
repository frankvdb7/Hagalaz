using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using MassTransit;
using Microsoft.Extensions.Localization;
using Hagalaz.Exceptions;
using Hagalaz.Services.Contacts.Store;
using Hagalaz.Services.Contacts.Store.Model;
using System.Linq;

namespace Hagalaz.Services.Contacts.Services
{
    public class ContactSessionService : IContactSessionService
    {
        private readonly ICharacterService _characterService;
        private readonly ContactSessionStore _contacts;
        private readonly WorldSessionStore _worlds;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IStringLocalizer<ContactSessionService> _stringLocalizer;

        public ContactSessionService(ICharacterService characterService, ContactSessionStore contacts, WorldSessionStore worlds, IPublishEndpoint publishEndpoint, IStringLocalizer<ContactSessionService> stringLocalizer)
        {
            _characterService = characterService;
            _contacts = contacts;
            _worlds = worlds;
            _publishEndpoint = publishEndpoint;
            _stringLocalizer = stringLocalizer;
        }

        public async Task AddLobbySession(int worldId, uint masterId)
        {
            var character = await _characterService.FindCharacterByIdAsync(masterId);
            if (character == null)
            {
                return;
            }
            var worldName = _stringLocalizer["Lobby"];
            if (!_contacts.TryAdd(masterId, new ContactSessionContext
            (
                masterId,
                worldId,
                worldName
            )))
            {
                return;
            }
            await _publishEndpoint.Publish(new ContactSignInMessage(new ContactDto
            {
                MasterId = masterId,
                DisplayName = character.DisplayName,
                PreviousDisplayName = character.PreviousDisplayName,
                WorldId = worldId,
                WorldName = worldName
            }));
        }

        public async Task AddWorldSession(int worldId, uint masterId)
        {
            var worldName = _worlds.TryGetValue(worldId, out var world) ? world.WorldName : throw new NotFoundException(nameof(world));
            var character = await _characterService.FindCharacterByIdAsync(masterId);
            if (character == null)
            {
                return;
            }
            if (!_contacts.TryAdd(masterId, new ContactSessionContext
            (
                masterId,
                worldId,
                worldName
            )))
            {
                return;
            }
            await _publishEndpoint.Publish(new ContactSignInMessage(new ContactDto
            {
                MasterId = masterId,
                DisplayName = character.DisplayName,
                PreviousDisplayName = character.PreviousDisplayName,
                WorldId = worldId,
                WorldName = worldName
            }));
        }

        public async Task RemoveSession(uint masterId)
        {
            await RemoveSession(masterId, expectedSession: null);
        }

        public async Task RemoveWorldSessions(int worldId)
        {
            var sessions = _contacts
                .Where(session => session.WorldId == worldId)
                .ToList();

            foreach (var session in sessions)
            {
                await RemoveSession(session.MasterId, session);
            }
        }

        private async Task RemoveSession(uint masterId, ContactSessionContext? expectedSession)
        {
            var removed = expectedSession == null
                ? _contacts.TryRemove(masterId)
                : _contacts.TryRemove(masterId, expectedSession);
            if (!removed)
            {
                return;
            }

            await PublishSignOut(masterId);
        }

        private async Task PublishSignOut(uint masterId)
        {
            var character = await _characterService.FindCharacterByIdAsync(masterId);
            if (character == null)
            {
                return;
            }
            await _publishEndpoint.Publish(new ContactSignOutMessage(new ContactDto
            {
                MasterId = masterId,
                DisplayName = character.DisplayName,
                PreviousDisplayName = character.PreviousDisplayName
            }));
        }
    }
}
