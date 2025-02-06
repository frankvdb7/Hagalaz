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
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for drink allow event.
    /// If at least one event handler will
    /// catch this event drinking item
    /// will not be allowed.
    /// </summary>
    public class DrinkAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Drink
        /// </summary>
        /// <value>The item.</value>
        public IItem Drink { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrinkAllowEvent"/> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="drink">The drink.</param>
        public DrinkAllowEvent(ICharacter c, IItem drink) : base(c) => Drink = drink;
    }
}