namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the different types of messages that can be displayed in the chatbox or console.
    /// </summary>
    public enum ChatMessageType : short
    {
        /// <summary>
        /// A standard message displayed in the chatbox.
        /// </summary>
        ChatboxText = 0,
        /// <summary>
        /// A private message received from another player.
        /// </summary>
        PrivateMessageReceived = 3,
        /// <summary>
        /// A system message related to private messaging, displayed in white text.
        /// </summary>
        PrivateMessageWhiteText = 4,
        /// <summary>
        /// An error or alert message related to private messaging, displayed in red text.
        /// </summary>
        PrivateMessageRedText = 5,
        /// <summary>
        /// A private message sent by the player.
        /// </summary>
        PrivateMessageSend = 6,
        /// <summary>
        /// An error or alert message related to a sent private message, displayed in red text.
        /// </summary>
        PrivateMessageFromRedText = 7,
        /// <summary>
        /// A system message in a friends chat, displayed in blue text.
        /// </summary>
        FriendsChatBlueText = 9,
        /// <summary>
        /// A standard message in a friends chat, displayed in white text.
        /// </summary>
        FriendsChatWhiteText1 = 10,
        /// <summary>
        /// An alternate standard message in a friends chat, displayed in white text.
        /// </summary>
        FriendsChatWhiteText2 = 11,
        /// <summary>
        /// A message in a clan chat, displayed in white text.
        /// </summary>
        ClanChatWhiteText = 43,
        /// <summary>
        /// A message representing a console command that was entered.
        /// </summary>
        ConsoleCommandText = 98,
        /// <summary>
        /// A standard message displayed in the developer console.
        /// </summary>
        ConsoleText = 99,
        /// <summary>
        /// A trade request message from another player.
        /// </summary>
        TradeRequestMessage = 100,
        /// <summary>
        /// A duel request message from another player.
        /// </summary>
        DuelRequestMessage = 101,
        /// <summary>
        /// An assist request message from another player.
        /// </summary>
        GiveAssistance = 102,
        /// <summary>
        /// A standard chatbox message that has been filtered by the game's profanity filter.
        /// </summary>
        ChatBoxTextFiltered = 109,
        /// <summary>
        /// A request related to founding or joining a clan.
        /// </summary>
        ClanRequest = 117,
    }
}
