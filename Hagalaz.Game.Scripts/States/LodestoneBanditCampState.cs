using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.States;

namespace Hagalaz.Game.Scripts.States
{
    [StateId("lodestone-bandit-camp")]
    public class LodestoneBanditCampState : StateBase
    {
        public override bool IsSerializable() => true;

        public override void OnStateAdded(IState state, ICreature creature)
        {
            if (creature is ICharacter character)
            {
                character.Configurations.SendBitConfiguration(358, 15);
            }
        }
    }
}
