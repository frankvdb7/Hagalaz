using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.States;

namespace Hagalaz.Game.Scripts.States
{
    /// <summary>
    /// </summary>
    [StateScriptMetaData([
        StateType.LodestoneAlkharid, StateType.LodestoneArdougne, StateType.LodestoneBanditCamp, StateType.LodestoneBurthorpe, StateType.LodestoneCatherby,
        StateType.LodestoneDraynor, StateType.LodestoneEdgeville, StateType.LodestoneFalador, StateType.LodestoneLumbridge, StateType.LodestoneLunarIsle,
        StateType.LodestonePortSarim, StateType.LodestoneSeersVillage, StateType.LodestoneTaverley, StateType.LodestoneVarrock, StateType.LodestoneYanille
    ])]
    public class LodestoneState : StateScriptBase
    {
        /// <summary>
        ///     Determines whether this instance is serializable.
        /// </summary>
        /// <returns></returns>
        public override bool IsSerializable() => true;

        /// <summary>
        ///     Gets called when the state is added.
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public override void OnStateAdded(IState state, ICreature creature)
        {
            if (creature is ICharacter character)
            {
                var bitFileID = 0;
                var bitValue = 0;
                if (state.StateType == StateType.LodestoneBanditCamp)
                {
                    bitFileID = 358;
                    bitValue = 15;
                }
                else if (state.StateType == StateType.LodestoneLunarIsle)
                {
                    bitFileID = 2448;
                    bitValue = 190;
                }
                else
                {
                    bitFileID = 10782 + (int)state.StateType;
                    bitValue = 1;
                }

                character.Configurations.SendBitConfiguration(bitFileID, bitValue);
            }
        }
    }
}