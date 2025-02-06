// ***********************************************************************
// Assembly         : Hagalaz.Game
// Author           : Frank
// Created          : 06-08-2012
//
// Last Modified By : Frank
// Last Modified On : 09-26-2011
// ***********************************************************************
// <copyright file="MouseClickEvent.cs" company="">
//     . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************


using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character.Packet
{
    /// <summary>
    /// Class for mouse click event.
    /// </summary>
    public class MouseClickEvent : CharacterEvent
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
        /// Contains boolean if this click was left click
        /// if it's false it means that click was right.
        /// </summary>
        /// <value><c>true</c> if [left click]; otherwise, <c>false</c>.</value>
        public bool LeftClick { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseClickEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="timePassed">The time passed.</param>
        /// <param name="screenPosX">The screen pos X.</param>
        /// <param name="screenPosY">The screen pos Y.</param>
        /// <param name="leftClick">if set to <c>true</c> [left click].</param>
        public MouseClickEvent(ICharacter target, short timePassed, ushort screenPosX, ushort screenPosY, bool leftClick) : base(target)
        {
            TimePassed = timePassed;
            ScreenPosX = screenPosX;
            ScreenPosY = screenPosY;
            LeftClick = leftClick;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() => "MouseClick(" + TimePassed + "," + ScreenPosX + "," + ScreenPosY + "," + LeftClick + ")";
    }
}