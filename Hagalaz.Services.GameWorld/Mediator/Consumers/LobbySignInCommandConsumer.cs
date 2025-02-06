using System.Threading.Tasks;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Game.Messages.Protocol;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Mediator.Consumers
{
    public class LobbySignInCommandConsumer : IConsumer<LobbySignInCommand>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IScopedGameMediator _gameMediator;
        private readonly IOptions<WorldOptions> _options;

        public LobbySignInCommandConsumer(IBus publishEndpoint, IScopedGameMediator gameMediator, IOptions<WorldOptions> options)
        {
            _publishEndpoint = publishEndpoint;
            _gameMediator = gameMediator;
            _options = options;
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
            await Task.WhenAll(_gameMediator.SendAsync(new SendWorldInfoCommand(session)),
                _publishEndpoint.Publish(new GetContactsRequest(command.MasterId)),
                _publishEndpoint.Publish(new LobbyUserSignInMessage(command.MasterId, options.Id)));
        }
    }
}