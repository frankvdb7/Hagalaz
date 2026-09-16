using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Hubs.Filters;
using Microsoft.AspNetCore.Authorization;
using Raido.Common.Protocol;
using Raido.Server;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Hubs
{
    [Authorize]
    [CharacterFilter]
    public class NpcHub : RaidoHub
    {
        private readonly INpcService _npcService;
        private readonly INpcStore _npcStore;

        public NpcHub(INpcService npcService, INpcStore npcStore)
        {
            _npcService = npcService;
            _npcStore = npcStore;
        }

        [RaidoMessageHandler(typeof(NpcClickMessage))]
        public async Task OnNpcClick(NpcClickMessage message)
        {
            if (message.Index < 0 || message.Index >= short.MaxValue)
            {
                return;
            }
            var npc = await _npcService.FindByIndexAsync(message.Index);
            if (npc == null)
            {
                return;
            }
            var character = Context.GetCharacter();
            character.QueueTask(new RsTask(() =>
            {
                if (_npcStore.Contains(npc) && character.Viewport.VisibleCreatures.Contains(npc))
                {
                    npc.Script.OnCharacterClick(character, message.ClickType, message.ForceRun);
                }
            }, 1));
        }

    }
}
