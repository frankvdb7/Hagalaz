// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Options;
// using Hagalaz.Game.Configuration;
// using Hagalaz.Game.Model.Events;
// using Hagalaz.Game.Network.Game.Outgoing;
// using Hagalaz.Game.Abstractions.Features.Clans;
// using Hagalaz.Game.Abstractions.Store;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     /// <summary>
//     /// </summary>
//     /// <seealso cref="IMasterPacketHandler" />
//     public class NotifyClanSettingsChangedPacketHandler : ClanSettingsPacketHandlerBase, IMasterPacketHandler
//     {
//         /// <summary>
//         ///     The character repository
//         /// </summary>
//         private readonly ICharacterStore _characterStore;
//
//         private readonly IClanService _clanManager;
//         private readonly IOptions<WorldOptions> _worldOptions;
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="ClanSettingsPacketHandlerBase" /> class.
//         /// </summary>
//         /// <param name="characterStore">The character repository.</param>
//         /// <param name="clanManager"></param>
//         /// <param name="worldOptions"></param>
//         public NotifyClanSettingsChangedPacketHandler(ICharacterStore characterStore, IClanService clanManager, IOptions<WorldOptions> worldOptions)
//         {
//             _characterStore = characterStore;
//             _clanManager = clanManager;
//             _worldOptions = worldOptions;
//         }
//
//         /// <summary>
//         ///     Gets the opcode.
//         /// </summary>
//         /// <value>
//         ///     The opcode.
//         /// </value>
//         public int Opcode => 1245151;
//
//         /// <summary>
//         ///     Called when [handled].
//         /// </summary>
//         /// <param name="clanName">Name of the clan.</param>
//         /// <param name="settings">The settings.</param>
//         protected override async Task OnSettingsRead(string clanName, IClanSettings settings)
//         {
//             IClan clan = _clanManager.GetClanByName(clanName);
//             if (clan != null)
//             {
//                 _clanManager.PutClanSettings(clan, settings);
//                 ClanPacketComposer clanPacket = new ClanPacketComposer(clan, true);
//                 foreach (var member in clan.Members.Where(e => e.WorldId == _worldOptions.Value.Id))
//                 {
//                     //var character = await _characterStore.FindAllAsync().Where(c => c.MasterId == member.MasterId).SingleOrDefaultAsync();
//                     //character?.Session.SendPacketAsync(clanPacket);
//                 }
//
//                 new ClanSettingsUpdatedEvent(clan).Send();
//             }
//         }
//     }
// }