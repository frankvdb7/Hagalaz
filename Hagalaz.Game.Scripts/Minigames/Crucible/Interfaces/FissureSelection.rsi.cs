using System.Linq;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.Crucible.Interfaces
{
    /// <summary>
    /// </summary>
    public class FissureSelection : WidgetScript
    {
        public FissureSelection(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

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
            OnComponentClick bankClick = (componentID, clickType, info1, info2) =>
            {
                var selected = Crucible.Banks.SingleOrDefault(e => e.ComponentID == componentID);
                if (selected != null)
                {
                    Crucible.TeleportToFissure(Owner, selected);
                }

                InterfaceInstance.Close();
                return true;
            };
            for (short i = 4; i <= 7; i++)
            {
                InterfaceInstance.AttachClickHandler(i, bankClick);
            }

            OnComponentClick fissureClick = (componentID, clickType, info1, info2) =>
            {
                var selected = Crucible.Fissures.SingleOrDefault(e => e.ComponentID == componentID);
                if (selected != null)
                {
                    Crucible.TeleportToFissure(Owner, selected);
                }

                InterfaceInstance.Close();
                return true;
            };
            for (short i = 8; i <= 16; i++)
            {
                InterfaceInstance.AttachClickHandler(i, fissureClick);
            }
        }
    }
}