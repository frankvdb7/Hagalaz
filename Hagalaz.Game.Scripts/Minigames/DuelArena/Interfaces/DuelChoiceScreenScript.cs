using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena.Interfaces
{
    /// <summary>
    /// </summary>
    public class DuelChoiceScreenScript : WidgetScript
    {
        public DuelChoiceScreenScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     The callback.
        /// </summary>
        public Action<bool>? Callback { get; set; }

        /// <summary>
        ///     The target
        /// </summary>
        public ICharacter Target { get; set; }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            // draw this character info
            InterfaceInstance.DrawCharacterHead(8);
            InterfaceInstance.DrawString(9, Owner.DisplayName);
            InterfaceInstance.DrawString(10, "Combat level: " + Owner.Statistics.FullCombatLevel);

            // draw target character info
            InterfaceInstance.DrawCharacterHead(15, Target);
            InterfaceInstance.DrawString(16, Target.DisplayName);
            InterfaceInstance.DrawString(17, "Combat level: " + Target.Statistics.FullCombatLevel);

            InterfaceInstance.AttachClickHandler(25, (childId, clickType, extra1, extra2) =>
            {
                Callback?.Invoke(false);

                Owner.Widgets.CloseWidget(InterfaceInstance);
                return true;
            });
            InterfaceInstance.AttachClickHandler(26, (childId, clickType, extra1, extra2) =>
            {
                Callback?.Invoke(true);

                Owner.Widgets.CloseWidget(InterfaceInstance);
                return true;
            });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() => Callback = null;
    }
}