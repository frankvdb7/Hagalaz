using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena.Interfaces
{
    /// <summary>
    /// </summary>
    public class DuelEndScreenScript : WidgetScript
    {
        public DuelEndScreenScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     The opponent
        /// </summary>
        public ICharacter Opponent { get; set; }

        /// <summary>
        ///     The victorious.
        /// </summary>
        public bool Victorious { get; set; }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            Owner.Configurations.SendGlobalCs2String(359, Victorious ? "You were victorious!" : "You were defeated"); // title
            if (Opponent.IsDestroyed)
            {
                Owner.Configurations.SendGlobalCs2String(377, "(Gone offline)"); // name
            }

            InterfaceInstance.DrawString(11, Opponent.Statistics.FullCombatLevel.ToString());
            InterfaceInstance.DrawString(13, Victorious ? "The spoils:" : "You lost:");
            InterfaceInstance.AttachClickHandler(15, (componentID, clickType, itemID, itemSlot) =>
            {
                if (clickType != ComponentClickType.SpecialClick)
                {
                    return false;
                }

                if (Opponent.IsDestroyed)
                {
                    InterfaceInstance.Close();
                }
                else
                {
                    Owner.ForceRunMovementType(Owner.Profile.GetValue<bool>(ProfileConstants.RunSettingsToggled));
                    Owner.FaceLocation(Opponent);
                    Owner.Combat.SetTarget(Opponent);
                }

                return true;
            });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }
    }
}