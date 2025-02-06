using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Mining
{
    /// <summary>
    ///     Represents an exhausted rock.
    /// </summary>
    public class ExhaustedRocks : GameObjectScript
    {
        /// <summary>
        ///     The message displayed when examining the rock.
        /// </summary>
        public static readonly string NoOre = "There is no ore currently available in this rock.";

        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize()
        {
        }

        /// <summary>
        ///     Happens on character click.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType) => clicker.SendChatMessage(NoOre);
    }
}