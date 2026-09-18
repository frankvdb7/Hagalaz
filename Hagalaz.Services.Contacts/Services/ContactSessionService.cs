using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using MassTransit;
using Microsoft.Extensions.Localization;
using Hagalaz.Services.Contacts.Store;
using Hagalaz.Services.Contacts.Store.Model;

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

        public async Task AddLobbySession(
            int worldId,
            string worldInstanceId,
            long worldGeneration,
            uint masterId,
            long sessionGeneration,
            string connectionId)
        {
            if (!_worlds.TryGetAvailableExact(worldId, worldInstanceId, worldGeneration, out _))
            {
                return;
            }

            var character = await _characterService.FindCharacterByIdAsync(masterId);
            if (character == null)
            {
                return;
            }
            var worldName = _stringLocalizer["Lobby"];
            var session = new ContactSessionContext
            (
                masterId,
                worldId,
                worldName,
                sessionGeneration,
                connectionId,
                worldInstanceId,
                worldGeneration
            );
            if (!_contacts.TrySetNewerSession(session))
            {
                return;
            }

            if (!_worlds.TryGetAvailableExact(worldId, worldInstanceId, worldGeneration, out _))
            {
                _contacts.TryRemoveExact(session);
                return;
            }
            await _publishEndpoint.Publish(new ContactSignInMessage(new ContactDto
            {
                MasterId = masterId,
                DisplayName = character.DisplayName,
                PreviousDisplayName = character.PreviousDisplayName,
                WorldId = worldId,
                WorldName = worldName
            }, sessionGeneration, connectionId));
        }

        public async Task AddWorldSession(
            int worldId,
            string worldInstanceId,
            long worldGeneration,
            uint masterId,
            long sessionGeneration,
            string connectionId)
        {
            if (!_worlds.TryGetAvailableExact(worldId, worldInstanceId, worldGeneration, out var world))
            {
                return;
            }

            var worldName = world.WorldName;
            var character = await _characterService.FindCharacterByIdAsync(masterId);
            if (character == null)
            {
                return;
            }
            var session = new ContactSessionContext
            (
                masterId,
                worldId,
                worldName,
                sessionGeneration,
                connectionId,
                worldInstanceId,
                worldGeneration
            );
            if (!_contacts.TrySetNewerSession(session))
            {
                return;
            }

            if (!_worlds.TryGetAvailableExact(worldId, worldInstanceId, worldGeneration, out _))
            {
                _contacts.TryRemoveExact(session);
                return;
            }
            await _publishEndpoint.Publish(new ContactSignInMessage(new ContactDto
            {
                MasterId = masterId,
                DisplayName = character.DisplayName,
                PreviousDisplayName = character.PreviousDisplayName,
                WorldId = worldId,
                WorldName = worldName
            }, sessionGeneration, connectionId));
        }

        public async Task RemoveSession(uint masterId, long sessionGeneration, string connectionId)
        {
            if (!_contacts.TryRemoveExact(masterId, sessionGeneration, connectionId))
            {
                return;
            }

            await PublishSignOut(masterId, sessionGeneration, connectionId);
        }

        public async Task RemoveWorldSessions(int worldId, string worldInstanceId, long worldGeneration)
        {
            foreach (var session in _contacts.RemoveSessionsForWorld(worldId, worldInstanceId, worldGeneration))
            {
                await PublishSignOut(session.MasterId, session.SessionGeneration, session.ConnectionId);
            }
        }

        private async Task PublishSignOut(uint masterId, long sessionGeneration, string connectionId)
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
            }, sessionGeneration, connectionId));
        }
    }
}
