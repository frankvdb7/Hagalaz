using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character.Packet
{
    /// <summary>
    /// 
    /// </summary>
    public class MouseEvent : CharacterEvent
    {
        /// <summary>
        /// Contains how much time passed in ms.
        /// </summary>
        /// <value>The time passed.</value>
        public short TimePassed { get; }

        /// <summary>
        /// Contains screen position X
        /// that was clicked.
        /// </summary>
        /// <value>The screen pos X.</value>
        public ushort ScreenPosX { get; }

        /// <summary>
        /// Contains screen position Y
        /// that was clicked.
        /// </summary>
        /// <value>The screen pos Y.</value>
        public ushort ScreenPosY { get; }

        /// <summary>
        /// Contains the event key code.
        /// </summary>
        /// <value>
        /// The event key code.
        /// </value>
        public byte EventKeyCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseClickEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="timePassed">The time passed.</param>
        /// <param name="screenPosX">The screen pos X.</param>
        /// <param name="screenPosY">The screen pos Y.</param>
        /// <param name="eventKeyCode">The event key code.</param>
        public MouseEvent(ICharacter target, short timePassed, ushort screenPosX, ushort screenPosY, byte eventKeyCode) : base(target)
        {
            TimePassed = timePassed;
            ScreenPosX = screenPosX;
            ScreenPosY = screenPosY;
            EventKeyCode = eventKeyCode;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() => "MouseEvent(" + TimePassed + "," + ScreenPosX + "," + ScreenPosY + "," + EventKeyCode + ")";
    }
}