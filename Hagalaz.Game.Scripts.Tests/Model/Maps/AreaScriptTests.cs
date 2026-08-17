using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Scripts.Model.Maps;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Model.Maps;

[TestClass]
public sealed class AreaScriptTests
{
    [TestMethod]
    public void Initialize_WithConfiguredWorldOptions_UsesConfiguredRespawnLocation()
    {
        var script = new TestAreaScript(Options.Create(new WorldOptions
        {
            SpawnPointX = 3200,
            SpawnPointY = 3201,
            SpawnPointZ = 2
        }));

        script.Initialize(Substitute.For<IArea>());

        var respawn = script.GetRespawnLocation(Substitute.For<ICharacter>());

        Assert.AreEqual(3200, respawn.X);
        Assert.AreEqual(3201, respawn.Y);
        Assert.AreEqual(2, respawn.Z);
    }

    private sealed class TestAreaScript(IOptions<WorldOptions> worldOptions) : AreaScript(worldOptions)
    {
        protected override void Initialize() { }
    }
}
