using System.Threading.Tasks;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Game.Messages.Mediator;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Mediator.Consumers
{
    public class WorldSignInCommandConsumer : IConsumer<WorldSignInCommand>
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IOptions<WorldOptions> _options;

        public WorldSignInCommandConsumer(IBus publishEndpoint, IOptions<WorldOptions> options)
        {
            _publishEndpoint = publishEndpoint;
            _options = options;
        }

        public async Task Consume(ConsumeContext<WorldSignInCommand> context)
        {
            var message = context.Message;
            var options = _options.Value;
            var onRegisteredTask = context.Message.Character.OnRegistered();
            var getContactsTask = _publishEndpoint.Publish(new GetContactsRequest(message.Character.MasterId));
            var userSignInTask = _publishEndpoint.Publish(new WorldUserSignInMessage(message.Character.MasterId, options.Id));
            await Task.WhenAll(onRegisteredTask, getContactsTask, userSignInTask);
        }
    }
}
