// ***********************************************************************
// Assembly         : Hagalaz.Game
// Author           : Frank
// Created          : 06-08-2012
//
// Last Modified By : Frank
// Last Modified On : 09-26-2011
// ***********************************************************************
// <copyright file="WalkAllowEvent.cs" company="">
//     . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************


using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for walking allow event.
    /// If at least one event handler will
    /// catch this event walking will not be allowed.
    /// </summary>
    public class WalkAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Contains target location.
        /// </summary>
        /// <value>The target location.</value>
        public ILocation TargetLocation { get; }

        /// <summary>
        /// Wheter CTRL key was pressed.
        /// </summary>
        /// <value><c>true</c> if [force run]; otherwise, <c>false</c>.</value>
        public bool ForceRun { get; }

        /// <summary>
        /// Wheter player clicked in minimap.
        /// </summary>
        /// <value><c>true</c> if minimap; otherwise, <c>false</c>.</value>
        public bool Minimap { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WalkAllowEvent" /> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="target">The target.</param>
        /// <param name="forceRun">if set to <c>true</c> [force run].</param>
        /// <param name="minimap">if set to <c>true</c> [minimap].</param>
        public WalkAllowEvent(ICharacter c, ILocation target, bool forceRun, bool minimap) : base(c)
        {
            TargetLocation = target;
            ForceRun = forceRun;
            Minimap = minimap;
        }
    }
}