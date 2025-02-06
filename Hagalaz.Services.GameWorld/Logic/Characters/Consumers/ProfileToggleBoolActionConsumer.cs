using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Providers;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Consumers
{
    public class ProfileToggleBoolActionConsumer : IConsumer<ProfileToggleBoolAction> 
    {
        private readonly ICharacterContextAccessor _contextAccessor;

        public ProfileToggleBoolActionConsumer(ICharacterContextAccessor contextAccessor) => _contextAccessor = contextAccessor;

        public async Task Consume(ConsumeContext<ProfileToggleBoolAction> context)
        {
            var character = _contextAccessor.Context.Character;
            var message = context.Message;
            var oldValue = character.Profile.GetValue<bool>(message.Key);
            var newValue = !oldValue;
            character.Profile.SetValue(message.Key, newValue);
            await context.Publish(new ProfileValueChanged<bool>(message.Key, newValue, oldValue), context.CancellationToken);
        }
    }
}
