using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.Model.Widgets
{
    /// <summary>
    /// Default Script class for interfaces.
    /// </summary>
    public class DefaultWidgetScript : WidgetScript
    {
        public DefaultWidgetScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        /// Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
        }

        /// <summary>
        /// Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }
    }
}