// ***********************************************************************
// Assembly         : Hagalaz.Game
// Author           : Frank
// Created          : 06-08-2012
//
// Last Modified By : Frank
// Last Modified On : 10-09-2011
// ***********************************************************************
// <copyright file="UnEquipAllowEvent.cs" company="">
//     . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for unequip allow event.
    /// If at least one event handler will
    /// catch this event unequiping given item
    /// will not be allowed.
    /// </summary>
    public class UnEquipAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Slot at which item does exist.
        /// </summary>
        /// <value>The item.</value>
        public IItem Item { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnEquipAllowEvent" /> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="item">The item.</param>
        public UnEquipAllowEvent(ICharacter c, IItem item) : base(c) => Item = item;
    }
}