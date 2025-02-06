using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Widgets.Warning;

namespace Hagalaz.Game.Scripts.Minigames.Crucible.Interfaces
{
    public class BountyInterfaceScript : WarningInterfaceScript
    {
        public BountyInterfaceScript(ICharacterContextAccessor characterContextAccessor)
            : base(characterContextAccessor)
        {
            AcceptComponentID = 21;
            DeclineComponentID = 20;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            base.OnOpen();

            InterfaceInstance.SetVisible(40, false);
            InterfaceInstance.SetVisible(41, false);
            InterfaceInstance.DrawString(23, "0");
            InterfaceInstance.DrawString(5, "0");
            InterfaceInstance.DrawString(6, "0");
            InterfaceInstance.DrawString(7, "0");
        }
    }
}