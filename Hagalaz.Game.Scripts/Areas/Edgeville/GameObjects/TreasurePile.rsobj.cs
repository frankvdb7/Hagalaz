using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Widgets.Rewards;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.GameObjects
{
    [GameObjectScriptMetaData([40951])]
    public class TreasurePile : GameObjectScript
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
            if (clickType == GameObjectClickType.Option1Click ||
                clickType == GameObjectClickType.Option2Click ||
                clickType == GameObjectClickType.Option3Click)
            {
                var rewardsScript = clicker.ServiceProvider.GetRequiredService<RewardsInterface>();
                clicker.Widgets.OpenWidget(645, 0, rewardsScript, false);
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