// ***********************************************************************
// Assembly         : Hagalaz.Game
// Author           : Frank
// Created          : 06-08-2012
//
// Last Modified By : Frank
// Last Modified On : 09-26-2011
// ***********************************************************************
// <copyright file="ConsoleCommandEvent.cs" company="">
//     . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************


using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character.Packet
{
    /// <summary>
    /// This event happens when character enters command into
    /// client console.
    /// </summary>
    public class ConsoleCommandEvent : CharacterEvent
    {
        /// <summary>
        /// Command which was entered.
        /// </summary>
        /// <value>The command.</value>
        public string Command { get; }

        /// <summary>
        /// Constructs new command enter event instance with given
        /// command text.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="command">Command which was entered.</param>
        public ConsoleCommandEvent(ICharacter target, string command) : base(target) => Command = command;
    }
}