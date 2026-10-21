using System.Threading.Tasks;
using System;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Game.Messages.Mediator;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Services.GameWorld.Mediator.Consumers
{
    public class WorldSignInCommandConsumer : IConsumer<WorldSignInCommand>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ContactSnapshotLoader _contactSnapshotLoader;
        private readonly IOptions<WorldOptions> _options;
        private readonly IMapRegionLoadScheduler _mapRegionLoadScheduler;
        private readonly IGameSessionConnectionTerminator _connectionTerminator;
        private readonly WorldInstanceIdentity _identity;
        private readonly ILogger<WorldSignInCommandConsumer> _logger;

        public WorldSignInCommandConsumer(
            IBus publishEndpoint,
            IRequestClient<GetContactsRequest> getContactsRequestClient,
            IOptions<WorldOptions> options,
            IMapRegionLoadScheduler mapRegionLoadScheduler,
            IGameSessionConnectionTerminator connectionTerminator,
            IGameConnectionService connectionService,
            IMapper mapper,
            WorldInstanceIdentity identity,
            ILogger<WorldSignInCommandConsumer> logger)
        {
            _publishEndpoint = publishEndpoint;
            _contactSnapshotLoader = new ContactSnapshotLoader(getContactsRequestClient, mapper, connectionService);
            _options = options;
            _mapRegionLoadScheduler = mapRegionLoadScheduler;
            _connectionTerminator = connectionTerminator;
            _identity = identity;
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
                character.Viewport.RefreshVisibleRegions();
                await _mapRegionLoadScheduler.EnsureLoadedAsync(character.Viewport.VisibleRegions, context.CancellationToken);
                character.OnRegistered();
                var observationWindow = await _contactSnapshotLoader.OpenObservationWindowAsync(session);
                var contactsTask = _contactSnapshotLoader.LoadAsync(
                    session,
                    observationWindow,
                    _logger,
                    context.CancellationToken);
                await Task.WhenAll(
                    contactsTask,
                    _publishEndpoint.Publish(new WorldUserSignInMessage(
                        character.MasterId,
                        options.Id,
                        _identity.InstanceId,
                        _identity.Generation,
                        session.SessionGeneration,
                        session.ConnectionId)));
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                throw;
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
