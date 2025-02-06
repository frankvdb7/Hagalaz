// ***********************************************************************
// Assembly         : Hagalaz.Game
// Author           : Frank
// Created          : 06-08-2012
//
// Last Modified By : Frank
// Last Modified On : 11-26-2011
// ***********************************************************************
// <copyright file="DropAllowEvent.cs" company="">
//     . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************


using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for special allow event.
    /// If at least one event handler will
    /// catch this event special
    /// will not be allowed.
    /// </summary>
    public class SpecialAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpecialAllowEvent"/> class.
        /// </summary>
        /// <param name="c">The c.</param>
        public SpecialAllowEvent(ICharacter c) : base(c)
        {
        }
    }
}