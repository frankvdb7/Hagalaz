using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Providers;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Consumers
{
    public class ProfileDecrementIntActionConsumer : IConsumer<ProfileDecrementIntAction>
    {
        private readonly ICharacterContextAccessor _contextAccessor;

        public ProfileDecrementIntActionConsumer(ICharacterContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task Consume(ConsumeContext<ProfileDecrementIntAction> context)
        {
            var character = _contextAccessor.Context.Character;
            var message = context.Message;
            var oldValue = character.Profile.GetValue<int>(message.Key);
            var newValue = oldValue - message.Value;
            if (newValue < message.MinValue)
            {
                newValue = message.MinValue;
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
