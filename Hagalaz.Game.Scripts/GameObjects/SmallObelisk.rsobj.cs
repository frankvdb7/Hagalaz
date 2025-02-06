using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects
{
    [GameObjectScriptMetaData([29940, 29941, 29942, 29943, 29944, 29945, 29947, 29954, 29955, 29956, 29957, 29958, 68237, 74830])]
    public class SmallObelisk : GameObjectScript
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
            if (clickType != GameObjectClickType.Option1Click)
            {
                base.OnCharacterClickPerform(clicker, clickType);
                return;
            }

            // TODO - Correct text and gfx.
            clicker.SendChatMessage(clicker.Statistics.HealSkill(StatisticsConstants.Summoning, 99) != 0
                ? "A magical force restored your summoning."
                : "You cannot restore any more summoning points.");
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize() { }
    }
}