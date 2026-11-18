using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Minigames.Barrows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Minigames.Barrows;

[TestClass]
public sealed class BarrowsRunLifecycleTests
{
    [TestMethod]
    public void Construction_UsesInjectedDependenciesAndRestoresKilledFlags()
    {
        var (profile, script) = Create(new BarrowsDto { AhrimKilled = true, KarilKilled = true, KillCount = 2, TunnelIndex = 3 });

        Assert.IsTrue(script.IsKilled(BarrowsConstants.AhrimIndex));
        Assert.IsTrue(script.IsKilled(BarrowsConstants.KarilIndex));
        Assert.IsFalse(script.IsKilled(BarrowsConstants.DharokIndex));
        profile.Received().SetObject(BarrowsConstants.MinigamesBarrows, Arg.Is<BarrowsDto>(d => d.TunnelIndex == 3 && d.AhrimKilled));
    }

    [TestMethod]
    public void Construction_WithoutProfileRandomizesTunnelAmongSixMoundsAndPersistsIt()
    {
        for (var i = 0; i < 50; i++)
        {
            var (profile, _) = Create(null);

            profile.Received().SetObject(BarrowsConstants.MinigamesBarrows,
                Arg.Is<BarrowsDto>(d => d.TunnelIndex >= 0 && d.TunnelIndex < BarrowsConstants.MoundCount));
        }
    }

    [TestMethod]
    public void OnRemove_BeforeClaimPreservesProgress()
    {
        var (profile, script) = Create(new BarrowsDto { VeracKilled = true, KillCount = 1, TunnelIndex = 2 });
        profile.ClearReceivedCalls();

        script.OnRemove();

        profile.DidNotReceive().SetObject(BarrowsConstants.MinigamesBarrows, Arg.Any<BarrowsDto>());
    }

    [TestMethod]
    public void OnRemove_AfterClaimStartsFreshRun()
    {
        var (profile, script) = Create(new BarrowsDto { VeracKilled = true, LootedChest = true, KillCount = 1, TunnelIndex = 2 });
        profile.ClearReceivedCalls();

        script.OnRemove();

        profile.Received(1).SetObject(BarrowsConstants.MinigamesBarrows,
            Arg.Is<BarrowsDto>(d => !d.VeracKilled && !d.LootedChest && d.KillCount == 0 && d.TunnelIndex < BarrowsConstants.MoundCount));
    }

    private static (IProfile Profile, BarrowsScript Script) Create(BarrowsDto? stored)
    {
        var profile = Substitute.For<IProfile>();
        profile.GetObject(BarrowsConstants.MinigamesBarrows, Arg.Any<BarrowsDto?>())
            .Returns(call => stored ?? call.ArgAt<BarrowsDto?>(1));
        var character = Substitute.For<ICharacter>();
        character.Profile.Returns(profile);
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);

        var script = new BarrowsScript(accessor, Substitute.For<IItemService>(), Substitute.For<ILootService>(),
            Substitute.For<INpcBuilder>(), Substitute.For<IHintIconBuilder>());
        return (profile, script);
    }
}
