using System;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Defines a set of flags that describe the properties and rules of a game world.
    /// </summary>
    [Flags]
    public enum WorldFlags
    {
        /// <summary>
        /// Indicates that no special flags are set for the world.
        /// </summary>
        None = 0,
        /// <summary>
        /// Indicates that the world is for members only.
        /// </summary>
        MembersOnly = 0x1,
        /// <summary>
        /// Indicates that the world allows the use of quick chat.
        /// </summary>
        QuickChatAllowed = 0x2,
        /// <summary>
        /// Indicates that the world has player-versus-player (PvP) combat enabled.
        /// </summary>
        PlayerVersusPlayer = 0x4,
        /// <summary>
        /// Indicates that the world allows loot sharing among players.
        /// </summary>
        LootShareAllowed = 0x8,
        /// <summary>
        /// Indicates that the world should be highlighted in the world selection list.
        /// </summary>
        HighLight = 0x16,
        /// <summary>
        /// Indicates that only game administrators can access the game world.
        /// </summary>
        GameAdminsOnly = 0x4000,
        /// <summary>
        /// Indicates that only game administrators can access the world's lobby.
        /// </summary>
        LobbyAdminsOnly = 0x2000,
    }
}
