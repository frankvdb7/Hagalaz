using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character.Packet
{
    /// <summary>
    /// Class for mouse move event.
    /// </summary>
    public class MouseMoveEvent : CharacterEvent
    {
        /// <summary>
        /// Contains how much time passed in ms.
        /// </summary>
        /// <value>The time passed.</value>
        public short TimePassed { get; }

        /// <summary>
        /// Contains delta X where the delta is between previous and
        /// current mouse position.
        /// </summary>
        /// <value>The delta X.</value>
        public short DeltaX { get; }

        /// <summary>
        /// Contains delta Y where the delta is between previous and
        /// current mouse position.
        /// </summary>
        /// <value>The delta Y.</value>
        public short DeltaY { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseMoveEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="timePassed">The time passed.</param>
        /// <param name="deltaX">The delta X.</param>
        /// <param name="deltaY">The delta Y.</param>
        public MouseMoveEvent(ICharacter target, short timePassed, short deltaX, short deltaY) : base(target)
        {
            TimePassed = timePassed;
            DeltaX = deltaX;
            DeltaY = deltaY;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() => "MouseMoveEvent(" + TimePassed + "," + DeltaX + "," + DeltaY + ")";
    }
}