using System.Threading.Tasks;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Game.Messages.Mediator;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Mediator.Consumers
{
    public class WorldSignOutCommandConsumer : IConsumer<WorldSignOutCommand>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IOptions<WorldOptions> _options;

        public WorldSignOutCommandConsumer(IBus publishEndpoint, IOptions<WorldOptions> options)
        {
            _publishEndpoint = publishEndpoint;
            _options = options;
        }

        public async Task Consume(ConsumeContext<WorldSignOutCommand> context)
        {
            var message = context.Message;
            var options = _options.Value;
            await _publishEndpoint.Publish(new WorldUserSignOutMessage(message.MasterId, options.Id));
        }
    }
}
