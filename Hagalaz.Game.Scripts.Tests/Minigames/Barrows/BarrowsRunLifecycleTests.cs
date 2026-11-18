using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
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
    public void SpawnBarrowBrother_UsesInjectedBuilders()
    {
        var (npcBuilder, hintIconBuilder, npcId, npcOptional, handle) = CreateSpawnableBuilders();
        var (_, script) = Create(new BarrowsDto { TunnelIndex = 3 }, npcBuilder, hintIconBuilder);

        script.SpawnBarrowBrother(BarrowsConstants.BarrowBrotherIDs[BarrowsConstants.AhrimIndex]);

        npcId.Received(1).WithId(BarrowsConstants.BarrowBrotherIDs[BarrowsConstants.AhrimIndex]);
        npcOptional.Received(1).Spawn();
        hintIconBuilder.Received(1).Create();
    }

    [TestMethod]
    public void Construction_PacksHudConfigurationWithoutChestAndKillCountOverlap()
    {
        var character = Substitute.For<ICharacter>();
        character.HasState<BarrowsOpenedChestState>().Returns(true);

        Create(new BarrowsDto { AhrimKilled = true, KarilKilled = true, KillCount = 3 }, character: character);

        character.Configurations.Received().SendStandardConfiguration(453, (1 << 0) | (1 << 3) | (1 << 16) | (3 << 17));
    }

    [TestMethod]
    public void SpawnBarrowBrother_AkrisaeStaysLockedWithoutQuestSource()
    {
        var npcBuilder = Substitute.For<INpcBuilder>();
        var (_, script) = Create(new BarrowsDto { TunnelIndex = 3 }, npcBuilder);

        script.SpawnBarrowBrother(BarrowsConstants.BarrowBrotherIDs[BarrowsConstants.AkrisaeIndex]);

        npcBuilder.DidNotReceive().Create();
    }

    [TestMethod]
    public void OnDeath_DespawnsActiveBrotherButKeepsProgress()
    {
        var (npcBuilder, hintIconBuilder, _, _, handle) = CreateSpawnableBuilders();
        var (profile, script) = Create(new BarrowsDto { TunnelIndex = 3, VeracKilled = true }, npcBuilder, hintIconBuilder);
        script.SpawnBarrowBrother(BarrowsConstants.BarrowBrotherIDs[BarrowsConstants.AhrimIndex]);
        profile.ClearReceivedCalls();

        script.OnDeath();

        handle.Received(1).Unregister();
        Assert.IsTrue(script.IsKilled(BarrowsConstants.VeracIndex));
        profile.DidNotReceive().SetObject(BarrowsConstants.MinigamesBarrows, Arg.Any<BarrowsDto>());
    }

    [TestMethod]
    public void OnDeath_KeepsAlreadyDeadBrotherSoKillCreditIsStillAwarded()
    {
        var (npcBuilder, hintIconBuilder, _, _, handle) = CreateSpawnableBuilders();
        handle.Npc.Combat.IsDead.Returns(true);
        var (_, script) = Create(new BarrowsDto { TunnelIndex = 3 }, npcBuilder, hintIconBuilder);
        script.SpawnBarrowBrother(BarrowsConstants.BarrowBrotherIDs[BarrowsConstants.AhrimIndex]);

        script.OnDeath();

        handle.DidNotReceive().Unregister();
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

    private static (INpcBuilder NpcBuilder, IHintIconBuilder HintIconBuilder, INpcId NpcId, INpcOptional NpcOptional, INpcHandle Handle)
        CreateSpawnableBuilders()
    {
        var npcBuilder = Substitute.For<INpcBuilder>();
        var hintIconBuilder = Substitute.For<IHintIconBuilder>();
        var npc = Substitute.For<INpc>();
        var handle = Substitute.For<INpcHandle>();
        handle.Npc.Returns(npc);
        var npcId = Substitute.For<INpcId>();
        var npcLocation = Substitute.For<INpcLocation>();
        var npcOptional = Substitute.For<INpcOptional>();
        npcBuilder.Create().Returns(npcId);
        npcId.WithId(Arg.Any<int>()).Returns(npcLocation);
        npcLocation.WithLocation(Arg.Any<ILocation>()).Returns(npcOptional);
        npcOptional.Spawn().Returns(handle);
        var hintType = Substitute.For<IHintIconType>();
                hintIconBuilder.Create().Returns(hintType);
        hintType.AtEntity(npc).Returns(Substitute.For<IHintIconEntityOptional>());
        return (npcBuilder, hintIconBuilder, npcId, npcOptional, handle);
    }

    private static (IProfile Profile, BarrowsScript Script) Create(
        BarrowsDto? stored, INpcBuilder? npcBuilder = null, IHintIconBuilder? hintIconBuilder = null,
        ICharacter? character = null)
    {
        var profile = Substitute.For<IProfile>();
        profile.GetObject(BarrowsConstants.MinigamesBarrows, Arg.Any<BarrowsDto?>())
            .Returns(call => stored ?? call.ArgAt<BarrowsDto?>(1));
        character ??= Substitute.For<ICharacter>();
        character.Profile.Returns(profile);
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);

        var script = new BarrowsScript(accessor, Substitute.For<IItemService>(), Substitute.For<ILootService>(),
            npcBuilder ?? Substitute.For<INpcBuilder>(), hintIconBuilder ?? Substitute.For<IHintIconBuilder>());
        return (profile, script);
    }
}
