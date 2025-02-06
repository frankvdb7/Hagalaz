using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.GameObjects
{
    [GameObjectScriptMetaData([10284])]
    public class RewardChest : GameObjectScript
    {
        /// <summary>
        ///     Happens when character click's this object and then walks to it
        ///     and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacterClick is overrided
        ///     than this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                var script = clicker.GetOrAddScript<BarrowsScript>();
                if (clicker.HasState(StateType.BarrowsOpenedChest)) // search
                {
                    script.LootChest(Owner);
                }
                else // open
                {
                    if (script.OpenChest())
                    {
                        clicker.Configurations.SendBitConfiguration(Owner.Definition.VarpBitFileId, 1);
                    }
                }
            }
            else if (clickType == GameObjectClickType.Option2Click) // close
            {
                clicker.Configurations.SendBitConfiguration(Owner.Definition.VarpBitFileId, 0);
                clicker.RemoveState(StateType.BarrowsOpenedChest);
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
            }
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}