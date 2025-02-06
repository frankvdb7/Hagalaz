namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the message type.
    /// </summary>
    public enum ChatMessageType : short
    {
        /// <summary>
        /// 
        /// </summary>
        ChatboxText = 0,
        /// <summary>
        /// 
        /// </summary>
        PrivateMessageReceived = 3,
        /// <summary>
        /// 
        /// </summary>
        PrivateMessageWhiteText = 4,
        /// <summary>
        /// 
        /// </summary>
        PrivateMessageRedText = 5,
        /// <summary>
        /// 
        /// </summary>
        PrivateMessageSend = 6,
        /// <summary>
        /// 
        /// </summary>
        PrivateMessageFromRedText = 7,
        /// <summary>
        /// 
        /// </summary>
        FriendsChatBlueText = 9,
        /// <summary>
        /// The friends chat white text1
        /// </summary>
        FriendsChatWhiteText1 = 10,
        /// <summary>
        /// 
        /// </summary>
        FriendsChatWhiteText2 = 11,
        /// <summary>
        /// The clan chat white text
        /// </summary>
        ClanChatWhiteText = 43,
        ConsoleCommandText = 98,
        /// <summary>
        /// 
        /// </summary>
        ConsoleText = 99,
        /// <summary>
        /// 
        /// </summary>
        TradeRequestMessage = 100,
        /// <summary>
        /// 
        /// </summary>
        DuelRequestMessage = 101,
        /// <summary>
        /// 
        /// </summary>
        GiveAssistance = 102,
        /// <summary>
        /// 
        /// </summary>
        ChatBoxTextFiltered = 109,
        /// <summary>
        /// The clan found request
        /// </summary>
        ClanRequest = 117,
    }
}
