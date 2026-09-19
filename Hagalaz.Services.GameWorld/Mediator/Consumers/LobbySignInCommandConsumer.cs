using System.Threading.Tasks;
using System;
using System.Threading;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Features;
using MassTransit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Mediator.Consumers
{
    public class LobbySignInCommandConsumer : IConsumer<LobbySignInCommand>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IRequestClient<GetContactsRequest> _getContactsRequestClient;
        private readonly IScopedGameMediator _gameMediator;
        private readonly IGameConnectionService _connectionService;
        private readonly IOptions<WorldOptions> _options;
        private readonly WorldInstanceIdentity _identity;
        private readonly IMapper _mapper;
        private readonly ILogger<LobbySignInCommandConsumer> _logger;

        public LobbySignInCommandConsumer(
            IBus publishEndpoint,
            IRequestClient<GetContactsRequest> getContactsRequestClient,
            IScopedGameMediator gameMediator,
            IGameConnectionService connectionService,
            IOptions<WorldOptions> options,
            WorldInstanceIdentity identity,
            IMapper mapper,
            ILogger<LobbySignInCommandConsumer> logger)
        {
            _publishEndpoint = publishEndpoint;
            _getContactsRequestClient = getContactsRequestClient;
            _gameMediator = gameMediator;
            _connectionService = connectionService;
            _options = options;
            _identity = identity;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<LobbySignInCommand> context)
        {
            var command = context.Message;
            var session = command.GameSession;
            var options = _options.Value;
            session.SendMessage(new DrawFrameComponentMessage
            {
                Id = 906, ForceRedraw = false
            });
            session.SendMessage(new SetConfigMessage
            {
                Id = 2041, Value = 301989888
            }); // JAG - Jagex Account Guardian config
            session.SendMessage(new SetConfigMessage
            {
                Id = 2411, Value = 1
            }); // needed to open lobby frame
            session.SendMessage(new SetConfigMessage
            {
                Id = 2459, Value = 184549376
            }); // email validation screen
            session.SendMessage(new SetConfigMessage
            {
                Id = 2159, Value = 1
            }); // TODO: friends filter value
            session.SendMessage(new SetConfigMessage
            {
                Id = 2522, Value = 0
            }); // member trail popup
            session.SendMessage(new SetConfigMessage
            {
                Id = 2528, Value = 5636096
            }); // needed to open lobby frame
            session.SendMessage(new SetConfigMessage
            {
                Id = 2567, Value = 65
            }); // needed to open lobby frame
            var observationBoundary = 0L;
            var connection = await _connectionService.FindById(session.ConnectionId);
            var contacts = connection?.Features.Get<IContactsFeature>();
            if (contacts is not null)
            {
                observationBoundary = contacts.BeginObservationWindow();
            }

            var contactsTask = contacts is null
                ? Task.CompletedTask
                : LoadContactsAsync(session, contacts, observationBoundary, context.CancellationToken);
            await Task.WhenAll(_gameMediator.SendAsync(new SendWorldInfoCommand(session)), contactsTask,
                _publishEndpoint.Publish(new LobbyUserSignInMessage(
                    command.MasterId,
                    options.Id,
                    _identity.InstanceId,
                    _identity.Generation,
                    session.SessionGeneration,
                    session.ConnectionId)));
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
