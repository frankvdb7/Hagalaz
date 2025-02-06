using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services;
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

        public NpcHub(INpcService npcService)
        {
            _npcService = npcService;
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
            if (!character.Viewport.VisibleCreatures.Contains(npc))
            {
                return;
            }
            npc.Script.OnCharacterClick(character, message.ClickType, message.ForceRun);
        }

    }
}
