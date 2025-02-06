using System;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Contains world flag values.
    /// </summary>
    [Flags]
    public enum WorldFlags
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0,
        /// <summary>
        /// Wether the world is members only.
        /// </summary>
        MembersOnly = 0x1,
        /// <summary>
        /// Wether the world has quick chat enabled.
        /// </summary>
        QuickChatAllowed = 0x2,
        /// <summary>
        /// Whether the world has pvp enabled.
        /// </summary>
        PlayerVersusPlayer = 0x4,
        /// <summary>
        /// Wether the world has loot share enabled.
        /// </summary>
        LootShareAllowed = 0x8,
        /// <summary>
        /// Wether the world should be highlighted.
        /// </summary>
        HighLight = 0x16,
        /// <summary>
        /// Wheter game is for admins only.
        /// </summary>
        GameAdminsOnly = 0x4000,
        /// <summary>
        /// Wheter world lobby is for admins only.
        /// </summary>
        LobbyAdminsOnly = 0x2000,
    }
}
