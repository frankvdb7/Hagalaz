using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Dehydrators
{
    public class StateDehydrator : ICharacterDehydrator
    {
        public Task<CharacterModel> DehydrateAsync(ICharacter character, CharacterModel model)
        {
            var states = character.States.Select(s =>
            {
                var attribute = s.GetType().GetCustomAttribute<StateIdAttribute>();
                if (attribute == null)
                {
                    return null;
                }

                return new HydratedStateDto.StateEx { Id = attribute.Id, TicksLeft = s.TicksLeft };
            }).Where(s => s != null).ToList();

            return Task.FromResult(model with { State = new HydratedStateDto { StatesEx = states } });
        }
    }
}
