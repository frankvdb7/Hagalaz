using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Logic.Hydrators
{
    public class FamiliarHydrator(FamiliarRestorationState restorationState) : ICharacterHydrator
    {
        public void Hydrate(ICharacter character, CharacterModel model)
        {
            if (model.Familiar is not null)
            {
                restorationState.SetFamiliar(model.Familiar);
            }
        }
    }
}
