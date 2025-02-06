using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using MassTransit;

namespace Hagalaz.Services.Characters.Consumers
{
    public class UpdateCharacterRequestConsumer : IConsumer<UpdateCharacterRequest>
    {
        public Task Consume(ConsumeContext<UpdateCharacterRequest> context)
        {
            // TODO - implement

            return context.RespondAsync(new UpdateCharacterResponse(context.Message.CorrelationId, context.Message.MasterId));
        }
    }
}