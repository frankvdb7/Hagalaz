namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the different chat channels that the client can be in.
    /// </summary>
    public enum ClientChatType : byte
    {
        /// <summary>
        /// Standard public chat.
        /// </summary>
        Normal = 0,
        /// <summary>
        /// Friends chat channel.
        /// </summary>
        Friends = 1,
        /// <summary>
        /// Clan chat channel for members.
        /// </summary>
        Clan = 2,
        /// <summary>
        /// Clan chat channel for guests.
        /// </summary>
        ClanGuest = 3,
    }
}
