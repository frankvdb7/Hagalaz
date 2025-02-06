using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Providers;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Consumers
{
    public class ProfileIncrementIntActionConsumer : IConsumer<ProfileIncrementIntAction>
    {
        private readonly ICharacterContextAccessor _contextAccessor;

        public ProfileIncrementIntActionConsumer(ICharacterContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task Consume(ConsumeContext<ProfileIncrementIntAction> context)
        {
            var character = _contextAccessor.Context.Character;
            var message = context.Message;
            var oldValue = character.Profile.GetValue<int>(message.Key);
            long newValue = oldValue + message.Value;
            if (newValue > message.MaxValue)
            {
                newValue = message.MaxValue;
            }
            if (oldValue == newValue)
            {
                return;
            }
            character.Profile.SetValue(message.Key, newValue);
            await context.Publish(new ProfileValueChanged<int>(message.Key, message.Value, oldValue), context.CancellationToken);
        }
    }
}
