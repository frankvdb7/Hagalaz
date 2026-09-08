using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Extensions;
namespace Hagalaz.Services.GameWorld.Model.Creatures.Npcs
{
    public class NpcHandle : INpcHandle
    {
        private readonly INpcService _npcService;

        public NpcHandle(INpc npc, INpcService npcService)
        {
            Npc = npc;
            _npcService = npcService;
        }

        public INpc Npc { get; }

        public void Unregister()
        {
            Npc.QueueTask(() => _npcService.UnregisterAsync(Npc));
        }
    }
}
