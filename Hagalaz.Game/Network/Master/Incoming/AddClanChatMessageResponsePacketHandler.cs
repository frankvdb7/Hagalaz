// using System.Threading.Tasks;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Game.Abstractions.Store;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     /// <summary>
//     ///     Handler for receiving clan chat channel messages.
//     /// </summary>
//     public class AddClanChatMessageResponsePacketHandler : IMasterPacketHandler
//     {
//         /// <summary>
//         ///     The character repository
//         /// </summary>
//         private readonly ICharacterStore _characterStore;
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="AddClanChatMessageResponsePacketHandler" /> class.
//         /// </summary>
//         /// <param name="characterStore">The character repository.</param>
//         public AddClanChatMessageResponsePacketHandler(ICharacterStore characterStore) => _characterStore = characterStore;
//
//         /// <summary>
//         ///     Gets the opcode.
//         /// </summary>
//         /// <value>
//         ///     The opcode.
//         /// </value>
//         public int Opcode => 12412554;
//
//         /// <summary>
//         ///     Handles the packet.
//         /// </summary>
//         /// <param name="packet">The packet containing handle data.</param>
//         public async Task HandleAsync(Packet packet)
//         {
//             //var characters = new Dictionary<ICharacter, ClanRank>();
//
//             //byte memberCount = packet.ReadByte();
//             //for (int i = 0; i < memberCount; i++)
//             //{
//             //    long key = packet.ReadLong();
//             //    var character = await _characterStore.FindAllAsync().Where(c => c.Session.Id == key).SingleOrDefaultAsync();
//             //    ClanRank rank = (ClanRank)packet.ReadByte();
//             //    if (character != null)
//             //    {
//             //        characters.Add(character, rank);
//             //    }
//             //}
//
//             //string speaker = packet.ReadString();
//             //byte rights = packet.ReadByte();
//             //long uniqueId = packet.ReadLong();
//             //short messageLength = (short)packet.ReadSmart();
//             //byte[] encodedMessage = packet.GetRemainingData();
//
//             //await Task.WhenAll(characters.Keys.Select(c => c.Session.SendPacketAsync(new CcMessagePacketComposer(speaker, characters[c] > ClanRank.Guest, rights, uniqueId, messageLength, encodedMessage))));
//         }
//     }
// }