using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
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
            _npcService.UnregisterAsync(Npc).Wait();
        }
    }
}
