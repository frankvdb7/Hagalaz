// using System.Collections.Generic;
// using System.Globalization;
// using System.Linq;
// using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     /// <summary>
//     /// </summary>
//     /// <seealso cref="PacketComposer" />
//     public class AddClanRequestPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="AddClanRequestPacketComposer" /> class.
//         /// </summary>
//         /// <param name="owner">The owner.</param>
//         /// <param name="name">The name.</param>
//         /// <param name="founders">The founders.</param>
//         public AddClanRequestPacketComposer(ICharacter owner, string name, IEnumerable<ICharacter> founders)
//         {
//             this.SetOpcode(1337);
//             SetType(SizeType.Short);
//
//             //AppendInt((int)owner.MasterId);
//             var characters = founders.ToList();
//             AppendByte((byte)(characters.Count));
//             characters.ForEach(c =>
//             {
//                 //AppendInt((int)c.MasterId);
//             });
//
//             AppendString(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower()));
//         }
//     }
// }