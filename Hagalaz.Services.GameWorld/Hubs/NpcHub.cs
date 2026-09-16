using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
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
        private readonly IEntityService _entityService;

        public NpcHub(INpcService npcService, IEntityService entityService)
        {
            _npcService = npcService;
            _entityService = entityService;
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
            if (!_entityService.TryGetHandle(npc, out var npcHandle))
            {
                return;
            }
            var character = Context.GetCharacter();
            character.QueueTask(new RsTask(() =>
            {
                if (_entityService.TryResolve<INpc>(npcHandle, out var currentNpc)
                    && character.Viewport.VisibleCreatures.Contains(currentNpc))
                {
                    currentNpc!.Script.OnCharacterClick(character, message.ClickType, message.ForceRun);
                }
            }, 1));
        }

    }
}
