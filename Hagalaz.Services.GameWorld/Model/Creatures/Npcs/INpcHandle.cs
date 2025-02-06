using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Npcs
{
    public class NpcHandle : INpcHandle
    {
        public NpcHandle(INpc npc) => Npc = npc;

        public INpc Npc { get; }

        public void Unregister()
        {
            var npcService = Npc.ServiceProvider.GetRequiredService<INpcService>();
            npcService.UnregisterAsync(Npc).Wait();
        }
    }
}