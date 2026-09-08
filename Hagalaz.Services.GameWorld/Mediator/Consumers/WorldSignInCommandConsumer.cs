using System.Threading.Tasks;
using System;
using System.Threading;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Game.Messages.Mediator;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hagalaz.Services.GameWorld.Services;

namespace Hagalaz.Services.GameWorld.Mediator.Consumers
{
    public class WorldSignInCommandConsumer : IConsumer<WorldSignInCommand>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IOptions<WorldOptions> _options;
        private readonly ICharacterService _characterService;
        private readonly IGameSessionService _gameSessionService;
        private readonly MapRegionLoadScheduler _mapRegionLoadScheduler;
        private readonly IGameSessionConnectionTerminator _connectionTerminator;
        private readonly ILogger<WorldSignInCommandConsumer> _logger;

        public WorldSignInCommandConsumer(
            IBus publishEndpoint,
            IOptions<WorldOptions> options,
            ICharacterService characterService,
            IGameSessionService gameSessionService,
            MapRegionLoadScheduler mapRegionLoadScheduler,
            IGameSessionConnectionTerminator connectionTerminator,
            ILogger<WorldSignInCommandConsumer> logger)
        {
            _publishEndpoint = publishEndpoint;
            _options = options;
            _characterService = characterService;
            _gameSessionService = gameSessionService;
            _mapRegionLoadScheduler = mapRegionLoadScheduler;
            _connectionTerminator = connectionTerminator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<WorldSignInCommand> context)
        {
            var message = context.Message;
            var options = _options.Value;
            var character = message.Character;
            var session = character.Session;

            try
            {
                character.Viewport.RebuildView();
                await _mapRegionLoadScheduler.EnsureLoadedAsync(character.Viewport.VisibleRegions, context.CancellationToken);
                await character.OnRegistered();
                await Task.WhenAll(
                    _publishEndpoint.Publish(new GetContactsRequest(character.MasterId)),
                    _publishEndpoint.Publish(new WorldUserSignInMessage(character.MasterId, options.Id)));
            }
            catch (Exception exception)
            {
                try
                {
                    await CleanupFailedWorldSignInAsync(character, session, options.Id, exception);
                }
                finally
                {
                    _connectionTerminator.Abort(session);
                }

                throw;
            }
        }

        private async Task CleanupFailedWorldSignInAsync(
            ICharacter character,
            IGameSession session,
            int worldId,
            Exception registrationFailure)
        {
            _logger.LogError(
                registrationFailure,
                "World sign-in initialization failed for character '{masterId}'; cleaning up the character and session.",
                character.MasterId);

            if (!character.IsDestroyed)
            {
                try
                {
                    character.Destroy();
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to destroy character '{masterId}' after world sign-in initialization failed.",
                        character.MasterId);
                }
            }

            try
            {
                if (!await _characterService.RemoveAsync(character))
                {
                    _logger.LogWarning(
                        "Character '{masterId}' was not present in the character store during failed world sign-in cleanup.",
                        character.MasterId);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to remove character '{masterId}' after world sign-in initialization failed.",
                    character.MasterId);
            }

            try
            {
                await _gameSessionService.RemoveSession(session, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to remove game session '{connectionId}' after world sign-in initialization failed.",
                    session.ConnectionId);
            }
            finally
            {
                try
                {
                    await _gameSessionService.RemoveLocalSession(session);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to remove local game session '{connectionId}' after world sign-in initialization failed.",
                        session.ConnectionId);
                }
            }

            try
            {
                await _publishEndpoint.Publish(new WorldUserSignOutMessage(character.MasterId, worldId));
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to publish world sign-out cleanup for character '{masterId}'.",
                    character.MasterId);
            }
        }
    }
}
