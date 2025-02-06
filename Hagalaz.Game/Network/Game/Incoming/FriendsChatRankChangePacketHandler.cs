// using Hagalaz.Network.Common.Messages;

namespace Hagalaz.Game.Network.Game.Incoming
{
    /// <summary>
    ///     Handler for changing ranks in friends chat.
    /// </summary>
    public class FriendsChatRankChangePacketHandler : IGamePacketHandler
    {
        //private readonly ISqlDatabaseManager _databaseManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FriendsChatRankChangePacketHandler" /> class.
        /// </summary>
        /// <param name="databaseManager">The database manager.</param>
        //public FriendsChatRankChangePacketHandler(ISqlDatabaseManager databaseManager) => _databaseManager = databaseManager;

        /// <summary>
        ///     Gets or sets the opcode.
        /// </summary>
        /// <value>
        ///     The opcode.
        /// </value>
        public byte Opcode => 75;

        /// <summary>
        ///     Handles the packet.
        /// </summary>
        /// <param name="session">The session to handle packet for.</param>
        /// <param name="packet">The packet containing handle data.</param>
        // public async Task HandleAsync(IGameSession session, Packet packet)
        // {
        //     await Task.CompletedTask;
        //     string name = packet.ReadString();
        //     FriendsChatRank rank = (FriendsChatRank)packet.ReadByte();
        //
        //     // IContact contact = session.Character.ContactList.GetFriend(name);
        //     // if (contact != null)
        //     // {
        //     //     contact.FriendsChatRank = rank;
        //     //     using (ISqlDatabaseClient client = _databaseManager.GetClient())
        //     //     {
        //     //         client.AddParameter("c_id", contact.MasterId);
        //     //         client.AddParameter("rank", (sbyte)rank);
        //     //         client.ExecuteUpdate("UPDATE characters_contacts SET fc_rank=@rank WHERE id=@c_id;");
        //     //     }
        //     //
        //     //     UpdateFriendsPacketComposer fl = new UpdateFriendsPacketComposer();
        //     //     //fl.AddFriend(contact.DisplayName, contact.PreviousDisplayName, contact.WorldId, (byte)contact.FriendsChatRank, true, contact.InLobby);
        //     //     await session.SendPacketAsync(fl);
        //     // }
        // }
    }
}