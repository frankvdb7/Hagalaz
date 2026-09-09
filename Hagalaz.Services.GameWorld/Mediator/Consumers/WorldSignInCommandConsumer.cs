using System.Threading.Tasks;
using System;
using System.Threading;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Model;
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
        private readonly MapRegionLoadScheduler _mapRegionLoadScheduler;
        private readonly IGameSessionConnectionTerminator _connectionTerminator;
        private readonly ILogger<WorldSignInCommandConsumer> _logger;

        public WorldSignInCommandConsumer(
            IBus publishEndpoint,
            IOptions<WorldOptions> options,
            MapRegionLoadScheduler mapRegionLoadScheduler,
            IGameSessionConnectionTerminator connectionTerminator,
            ILogger<WorldSignInCommandConsumer> logger)
        {
            _publishEndpoint = publishEndpoint;
            _options = options;
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
                // The pre-registration rebuild is needed to discover all visible regions
                // before OnRegistered sends the initial character map.
                character.Viewport.RebuildView();
                await _mapRegionLoadScheduler.EnsureLoadedAsync(character.Viewport.VisibleRegions, context.CancellationToken);
                character.OnRegistered();
                await Task.WhenAll(
                    _publishEndpoint.Publish(new GetContactsRequest(character.MasterId)),
                    _publishEndpoint.Publish(new WorldUserSignInMessage(character.MasterId, options.Id, session.SessionGeneration, session.ConnectionId)));
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "World sign-in initialization failed for character '{masterId}'; aborting the connection.",
                    character.MasterId);
                _connectionTerminator.Abort(session);

                throw;
            }
        }
    }
}
