using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.GameObjects
{

    internal class KingBlackDragonWarning : WidgetScript
    {
        public KingBlackDragonWarning(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Raises the Close event.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.AttachClickHandler(13, (componentID, buttonClickType, info1, info2) =>
            {
                if (buttonClickType == ComponentClickType.SpecialClick || buttonClickType == ComponentClickType.LeftClick)
                {
                    Teleport();
                }

                return true;
            });
            InterfaceInstance.AttachClickHandler(14, (componentID, buttonClickType, info1, info2) =>
            {
                if (buttonClickType == ComponentClickType.SpecialClick || buttonClickType == ComponentClickType.LeftClick)
                {
                    InterfaceInstance.Close();
                }

                return true;
            });
        }

        /// <summary>
        ///     Teleports the specified clicker.
        /// </summary>
        private void Teleport()
        {
            new StandardTeleportScript(MagicBook.StandardBook, 2273, 4681, 0, 0).PerformTeleport(Owner);
            // TODO animation
            InterfaceInstance.Close();
        }
    }

    [GameObjectScriptMetaData([77834])]
    public class Artifact : GameObjectScript
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
                var script = clicker.ServiceProvider.GetRequiredService<KingBlackDragonWarning>();
                clicker.Widgets.OpenWidget(1361, 0, script, true);
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