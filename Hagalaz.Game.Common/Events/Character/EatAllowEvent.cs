using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for eat allow event.
    /// If at least one event handler will
    /// catch this event eating item
    /// will not be allowed.
    /// </summary>
    public class EatAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Food
        /// </summary>
        /// <value>The item.</value>
        public IItem Food { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EatAllowEvent"/> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="food">The food.</param>
        public EatAllowEvent(ICharacter c, IItem food) : base(c) => Food = food;
    }
}