using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena.Interfaces
{
    /// <summary>
    /// </summary>
    public class DuelConfirmScreenScript : WidgetScript
    {
        /// <summary>
        ///     Gets or sets the close callback.
        /// </summary>
        public Action? CloseCallback { get; set; }

        public DuelConfirmScreenScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) {}

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (CloseCallback != null)
            {
                CloseCallback();
            }
        }
    }
}