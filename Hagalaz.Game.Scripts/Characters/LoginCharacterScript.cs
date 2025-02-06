using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;
using Microsoft.Extensions.Options;

namespace Hagalaz.Game.Scripts.Characters
{
    /// <summary>
    ///     Standard script for character.
    /// </summary>
    public class LoginCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        private readonly IOptions<WorldOptions> _worldOptions;

        public LoginCharacterScript(ICharacterContextAccessor contextAccessor, IOptions<WorldOptions> worldOptions) : base(contextAccessor) => _worldOptions = worldOptions;

        /// <summary>
        ///     Happens when script instance is initialized.
        /// </summary>
        protected override void Initialize()
        {
        }

        /// <summary>
        ///     Happens when character enter's world.
        /// </summary>
        public override void OnRegistered()
        {
            if (!string.IsNullOrEmpty(_worldOptions.Value.WelcomeMessage))
            {
                Character.SendChatMessage(_worldOptions.Value.WelcomeMessage);
            }

            if (!string.IsNullOrEmpty(_worldOptions.Value.MessageOfTheWeek))
            {
                Character.SendChatMessage(_worldOptions.Value.MessageOfTheWeek);
            }
        }
    }
}