using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.Model.Widgets
{
    /// <summary>
    /// Default dialogue script.
    /// </summary>
    public class DefaultDialogueScript : DialogueScript
    {
        public DefaultDialogueScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        /// Happens when dialogue is opened for character.
        /// </summary>
        public override void OnOpen() { }

        /// <summary>
        /// Happens when dialogue is closed for character.
        /// </summary>
        public override void OnClose() { }
    }
}