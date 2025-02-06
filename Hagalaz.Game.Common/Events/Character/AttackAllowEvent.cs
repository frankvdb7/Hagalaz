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

using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for melee allow event.
    /// If at least one event handler will
    /// catch this event melee
    /// will not be allowed.
    /// </summary>
    public class AttackAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Contains the style.
        /// </summary>
        public AttackStyle Style { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackAllowEvent"/> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="style">The style.</param>
        public AttackAllowEvent(ICharacter c, AttackStyle style) : base(c) => Style = style;
    }
}