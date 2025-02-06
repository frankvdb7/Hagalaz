using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Clans
{
    /// <summary>
    /// </summary>
    public class ClanInviteScript : WidgetScript
    {
        /// <summary>
        ///     The inviter.
        /// </summary>
        public ICharacter Inviter { get; set;  }

        public ClanInviteScript(ICharacterContextAccessor characterContextAccessor): base(characterContextAccessor) {}

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
        }

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
        }
    }
}