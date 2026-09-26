using System.Threading.Tasks;
using System;
using System.Threading;
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
        private readonly IRequestClient<GetContactsRequest> _getContactsRequestClient;
        private readonly IOptions<WorldOptions> _options;
        private readonly IMapRegionLoadScheduler _mapRegionLoadScheduler;
        private readonly IGameSessionConnectionTerminator _connectionTerminator;
        private readonly IGameConnectionService _connectionService;
        private readonly IMapper _mapper;
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
            _getContactsRequestClient = getContactsRequestClient;
            _options = options;
            _mapRegionLoadScheduler = mapRegionLoadScheduler;
            _connectionTerminator = connectionTerminator;
            _connectionService = connectionService;
            _mapper = mapper;
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
                var observationBoundary = 0L;
                var connection = await _connectionService.FindById(session.ConnectionId);
                var contacts = connection?.Features.Get<IContactsFeature>();
                if (contacts is not null)
                {
                    observationBoundary = contacts.BeginObservationWindow();
                }

                var contactsTask = contacts is null
                    ? Task.CompletedTask
                    : LoadContactsAsync(
                        session,
                        contacts,
                        observationBoundary,
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

        private async Task LoadContactsAsync(
            IGameSession session,
            IContactsFeature contacts,
            long observationBoundary,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _getContactsRequestClient.GetResponse<GetContactsResponse>(
                    new GetContactsRequest(
                        session.MasterId,
                        session.SessionGeneration,
                        session.ConnectionId,
                        observationBoundary),
                    cancellationToken);
                if (!ContactSnapshotApplicator.TryApply(response.Message, session, contacts, _mapper))
                {
                    _logger.LogWarning(
                        "Discarded contacts snapshot for stale session '{ConnectionId}' and account '{MasterId}'.",
                        session.ConnectionId,
                        session.MasterId);
                }
            }
            catch (RequestTimeoutException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Contacts snapshot timed out for account '{MasterId}'; continuing sign-in without replacing contacts.",
                    session.MasterId);
            }
            catch (RequestFaultException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Contacts snapshot failed for account '{MasterId}'; continuing sign-in without replacing contacts.",
                    session.MasterId);
            }
            finally
            {
                contacts.CompleteInitialSnapshot();
                contacts.EndObservationWindow();
            }
        }
    }
}
