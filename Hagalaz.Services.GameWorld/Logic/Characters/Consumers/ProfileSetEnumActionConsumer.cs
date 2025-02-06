using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Providers;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Consumers
{
    public class ProfileSetEnumActionConsumer : IConsumer<ProfileSetEnumAction>
    {
        private readonly ICharacterContextAccessor _contextAccessor;

        public ProfileSetEnumActionConsumer(ICharacterContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task Consume(ConsumeContext<ProfileSetEnumAction> context)
        {
            var character = _contextAccessor.Context.Character;
            var message = context.Message;
            var oldValue = character.Profile.GetValue<int>(message.Key);
            var newValue = message.Value;
            if (oldValue.Equals(newValue))
            {
                return;
            }
            character.Profile.SetValue(message.Key, (int)(object)newValue);
            var valueChangedMessage = Activator.CreateInstance(typeof(ProfileValueChanged<>).MakeGenericType(message.Value.GetType()), message.Key, newValue, oldValue);
            await context.Publish(valueChangedMessage!, context.CancellationToken);
        }
    }
}
