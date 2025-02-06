// ***********************************************************************
// Assembly         : Hagalaz.Game
// Author           : Frank
// Created          : 07-21-2012
//
// Last Modified By : Frank
// Last Modified On : 07-21-2012
// ***********************************************************************
// <copyright file="ScreenChangedPacketHandler.cs" company="">
//     . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************


// using Hagalaz.Network.Common.Messages;

namespace Hagalaz.Game.Network.Game.Incoming
{
    /// <summary>
    ///     Handler for camera moved packet. (Most likely received when character pressed up, down, left and right keys.)
    /// </summary>
    public class CameraMovedPacketHandler : IGamePacketHandler
    {
        /// <summary>
        ///     Gets or sets the opcode.
        /// </summary>
        /// <value>
        ///     The opcode.
        /// </value>
        public byte Opcode => 33;

        /// <summary>
        ///     Handles packet.
        /// </summary>
        /// <param name="session">The session to handle packet for.</param>
        /// <param name="packet">The packet containing handle data.</param>
        // public Task HandleAsync(IGameSession session, Packet packet)
        // {
        //     short x = packet.ReadShortA(); // Occurs when character presses left or right.
        //     short y = packet.ReadLeShort(); // Occurs when character presses up or down.
        //     // TODO - Figure out how to handle this packet correctly.
        //     return Task.CompletedTask;
        // }
    }
}