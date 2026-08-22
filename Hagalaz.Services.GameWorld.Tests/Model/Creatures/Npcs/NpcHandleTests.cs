using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Creatures.Npcs;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Model.Creatures.Npcs
{
    [TestClass]
    public class NpcHandleTests
    {
        [TestMethod]
        public void Unregister_UsesInjectedNpcService()
        {
            var npc = Substitute.For<INpc>();
            var npcService = Substitute.For<INpcService>();
            npcService.UnregisterAsync(npc).Returns(Task.CompletedTask);
            var handle = new NpcHandle(npc, npcService);

            handle.Unregister();

            npcService.Received(1).UnregisterAsync(npc);
        }
    }
}
