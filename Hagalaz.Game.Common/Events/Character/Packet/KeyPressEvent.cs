// ***********************************************************************
// Assembly         : Hagalaz.Game
// Author           : Frank
// Created          : 06-08-2012
//
// Last Modified By : Frank
// Last Modified On : 09-26-2011
// ***********************************************************************
// <copyright file="KeyPressEvent.cs" company="">
//     . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************


using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character.Packet
{
    /// <summary>
    /// Class for key press event.
    /// </summary>
    public class KeyPressEvent : CharacterEvent
    {
        /// <summary>
        /// Key Id that was pressed.
        /// </summary>
        /// <value>The key Id.</value>
        public byte KeyID { get; }

        /// <summary>
        /// Contains how much time passed in ms.
        /// </summary>
        /// <value>The time passed.</value>
        public int TimePassed { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyPressEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="keyid">The keyid.</param>
        /// <param name="timepassed">The timepassed.</param>
        public KeyPressEvent(ICharacter target, byte keyid, int timepassed) : base(target)
        {
            KeyID = keyid;
            TimePassed = timepassed;
        }
    }
}