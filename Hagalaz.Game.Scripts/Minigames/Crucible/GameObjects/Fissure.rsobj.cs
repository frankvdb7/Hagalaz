using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Minigames.Crucible.Interfaces;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Crucible.GameObjects
{
    [GameObjectScriptMetaData([72923, 72924, 72925, 72926, 72927, 72928, 72929, 72930, 72931, 72932, 72933, 72934, 72935])]
    public class Fissure : GameObjectScript
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
            if (clickType == GameObjectClickType.Option1Click) // quick travel
            {
                Crucible.TeleportToFissure(clicker);
                return;
            }

            if (clickType == GameObjectClickType.Option2Click)
            {
                var fissure = clicker.ServiceProvider.GetRequiredService<FissureSelection>();
                clicker.Widgets.OpenWidget(1291, 0, fissure, true);
                return;
            }

            if (clickType == GameObjectClickType.Option3Click)
            {
                Crucible.TeleportToBank(clicker);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}