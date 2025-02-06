using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Providers;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Consumers
{
    public class ProfileSetObjectActionConsumer : IConsumer<ProfileSetObjectAction<object>>
    {
        private readonly ICharacterContextAccessor _contextAccessor;

        public ProfileSetObjectActionConsumer(ICharacterContextAccessor contextAccessor) => _contextAccessor = contextAccessor;

        public async Task Consume(ConsumeContext<ProfileSetObjectAction<object>> context)
        {
            var character = _contextAccessor.Context.Character;
            var message = context.Message;
            var oldObject = character.Profile.GetObject<object>(message.Key);
            var newObject = message.Object;
            var equals = oldObject?.Equals(newObject);
            if (equals.HasValue && equals.Value)
            {
                return;
            }
            character.Profile.SetObject(message.Key, newObject);
            var valueChangedMessage = Activator.CreateInstance(typeof(ProfileValueChanged<>).MakeGenericType(message.Object.GetType()), message.Key, newObject, oldObject);
            await context.Publish(valueChangedMessage!, context.CancellationToken);
        }
    }
}
