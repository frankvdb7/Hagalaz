using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Creatures.Npcs;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Model.Creatures.Npcs;

[TestClass]
public sealed class NpcHandleTests
{
    [TestMethod]
    public void Unregister_UsesSynchronousServiceOperation()
    {
        var npc = Substitute.For<INpc>();
        var npcService = Substitute.For<INpcService>();

        var handle = new NpcHandle(npc, npcService);

        handle.Unregister();

        npcService.Received(1).Unregister(npc);
        npcService.DidNotReceive().UnregisterAsync(npc);
    }
}
