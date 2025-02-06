// ***********************************************************************
// Assembly         : Hagalaz.Game
// Author           : Frank
// Created          : 06-08-2012
//
// Last Modified By : Frank
// Last Modified On : 09-26-2011
// ***********************************************************************
// <copyright file="WindowFocusEvent.cs" company="">
//     . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************


using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character.Packet
{
    /// <summary>
    /// Class for screen focus event.
    /// </summary>
    public class WindowFocusEvent : CharacterEvent
    {
        /// <summary>
        /// Contains bool if screen is active.
        /// Previous screen state is !ScreenActive
        /// </summary>
        /// <value><c>true</c> if [screen active]; otherwise, <c>false</c>.</value>
        public bool ScreenActive { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowFocusEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="screenActive">if set to <c>true</c> [screen active].</param>
        public WindowFocusEvent(ICharacter target, bool screenActive) : base(target) => ScreenActive = screenActive;
    }
}