using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Gets called when the familiar needs to be dismissed.
    /// </summary>
    public class FamiliarDismissEvent : CharacterEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FamiliarDismissEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public FamiliarDismissEvent(ICharacter target)
            : base(target)
        {
        }
    }
}