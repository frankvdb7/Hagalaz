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
    /// Class for drop allow event.
    /// If at least one event handler will
    /// catch this event droping given item
    /// will not be allowed.
    /// </summary>
    public class DropAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Slot at which item is stored in inventory.
        /// </summary>
        /// <value>The item.</value>
        public IItem Item { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DropAllowEvent" /> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="item">The item.</param>
        public DropAllowEvent(ICharacter c, IItem item) : base(c) => Item = item;
    }
}