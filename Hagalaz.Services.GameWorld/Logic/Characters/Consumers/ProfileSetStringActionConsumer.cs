using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Providers;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Consumers
{
    public class ProfileSetStringActionConsumer : IConsumer<ProfileSetStringAction>
    {
        private readonly ICharacterContextAccessor _contextAccessor;

        public ProfileSetStringActionConsumer(ICharacterContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task Consume(ConsumeContext<ProfileSetStringAction> context)
        {
            var character = _contextAccessor.Context.Character;
            var message = context.Message;
            var oldValue = character.Profile.GetValue(message.Key);
            var newValue = message.Value;
            if (oldValue == newValue)
            {
                return;
            }
            character.Profile.SetValue(message.Key, newValue);
            await context.Publish(new ProfileValueChanged<string>(message.Key, message.Value, oldValue), context.CancellationToken);
        }
    }
}
