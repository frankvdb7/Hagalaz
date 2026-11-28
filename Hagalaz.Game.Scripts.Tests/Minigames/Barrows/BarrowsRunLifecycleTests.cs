using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Scripts.Minigames.Barrows.GameObjects;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Minigames.Barrows;
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
        var (npcBuilder, hintIconBuilder, npcId, npcOptional, _) = CreateSpawnableBuilders();
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
    [DataRow(0, 64)]
    [DataRow(1, 128)]
    [DataRow(2, 256)]
    [DataRow(3, 512)]
    public void InCrypts_SetsOneRopeBitPerEntryQuadrant(int entryIndex, int expectedConfig)
    {
        var character = Substitute.For<ICharacter>();
        var (_, script) = Create(new BarrowsDto { CryptStartIndex = entryIndex, PuzzleSolved = true }, character: character);

        script.InCrypts = true;

        character.Configurations.Received().SendStandardConfiguration(452, expectedConfig);
    }

    [TestMethod]
    public void Construction_DoesNotWriteAkrisaeAsHudBrotherFlag()
    {
        var character = Substitute.For<ICharacter>();

        Create(new BarrowsDto { AkriseaKilled = true }, character: character);

        character.Configurations.Received().SendStandardConfiguration(453, 0);
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
    public void LootChest_ResetsRunAtSearchAndRejectsSecondClaim()
    {
        var (character, lootService, generator) = CreateClaimableChestCharacter();
        var chest = Substitute.For<IGameObject>();
        var (profile, script) = Create(new BarrowsDto { AhrimKilled = true, KillCount = 4, TunnelIndex = 2 }, character: character, lootService: lootService);
        profile.ClearReceivedCalls();

        script.LootChest(chest);
        script.LootChest(chest);

        Assert.IsFalse(script.IsKilled(BarrowsConstants.AhrimIndex));
        profile.Received(1).SetObject(BarrowsConstants.MinigamesBarrows,
            Arg.Is<BarrowsDto>(d => d.LootedChest && !d.AhrimKilled && d.KillCount == 0 && d.TunnelIndex < BarrowsConstants.MoundCount));
        // One slain brother means two rewards, rolled once even though the chest was searched twice.
        generator.Received(BarrowsRewards.RollCount(1)).GenerateLoot<ILootItem>(Arg.Any<LootParams>());
    }

    [TestMethod]
    public void LootChest_WithMissingTableDoesNotClaim()
    {
        var lootService = Substitute.For<ILootService>();
        lootService.FindGameObjectLootTable(Arg.Any<int>()).Returns(Task.FromResult<ILootTable?>(null));
        var character = Substitute.For<ICharacter>();
        character.HasState<BarrowsOpenedChestState>().Returns(true);
        var (profile, script) = Create(new BarrowsDto { AhrimKilled = true, TunnelIndex = 2 }, character: character, lootService: lootService);
        profile.ClearReceivedCalls();

        script.LootChest(Substitute.For<IGameObject>());

        Assert.IsTrue(script.IsKilled(BarrowsConstants.AhrimIndex));
        profile.DidNotReceive().SetObject(BarrowsConstants.MinigamesBarrows, Arg.Any<BarrowsDto>());
    }

    [TestMethod]
    public void OpenChest_AfterClaimDoesNotSpawnTunnelBrother()
    {
        var npcBuilder = Substitute.For<INpcBuilder>();
        var (_, script) = Create(new BarrowsDto { LootedChest = true, TunnelIndex = 2 }, npcBuilder);

        var opened = script.OpenChest();

        Assert.IsTrue(opened);
        npcBuilder.DidNotReceive().Create();
    }

    [TestMethod]
    public void OpenChest_BeforeClaimSpawnsUndefeatedTunnelBrother()
    {
        var (npcBuilder, hintIconBuilder, npcId, _, _) = CreateSpawnableBuilders();
        var (_, script) = Create(new BarrowsDto { TunnelIndex = 2 }, npcBuilder, hintIconBuilder);

        var opened = script.OpenChest();

        Assert.IsFalse(opened);
        npcId.Received(1).WithId(BarrowsConstants.BarrowBrotherIDs[2]);
    }

    [TestMethod]
    public void KillCredit_GoesOnlyToTheBrotherSpawnedForThisPlayer()
    {
        var (npcBuilder, hintIconBuilder, _, _, handle) = CreateSpawnableBuilders();
        EventHappened<CreatureKillEvent>? handler = null;
        var character = Substitute.For<ICharacter>();
        character.RegisterEventHandler(Arg.Do<EventHappened<CreatureKillEvent>>(h => handler = h));
        var (_, script) = Create(new BarrowsDto { TunnelIndex = 3 }, npcBuilder, hintIconBuilder, character);
        script.SpawnBarrowBrother(BarrowsConstants.BarrowBrotherIDs[BarrowsConstants.AhrimIndex]);
        var otherPlayersAhrim = Substitute.For<INpc>();
        otherPlayersAhrim.Appearance.CompositeID.Returns(BarrowsConstants.BarrowBrotherIDs[BarrowsConstants.AhrimIndex]);
        handle.Npc.Appearance.CompositeID.Returns(BarrowsConstants.BarrowBrotherIDs[BarrowsConstants.AhrimIndex]);

        handler!(new CreatureKillEvent(character, otherPlayersAhrim));
        var creditedForOther = script.IsKilled(BarrowsConstants.AhrimIndex);
        handler(new CreatureKillEvent(character, handle.Npc));

        Assert.IsFalse(creditedForOther);
        Assert.IsTrue(script.IsKilled(BarrowsConstants.AhrimIndex));
    }

    [TestMethod]
    [DataRow(0, 80)]
    [DataRow(2, 100)]
    public void Tick_DrainsPrayerFromRestoredKilledFlags(int killedBrothers, int expectedDrain)
    {
        var character = Substitute.For<ICharacter>();
        var (_, script) = Create(new BarrowsDto { AhrimKilled = killedBrothers > 0, KarilKilled = killedBrothers > 1 }, character: character);
        script.InCrypts = true;

        for (var i = 0; i < 30; i++)
        {
            script.Tick();
        }

        character.Statistics.Received(1).DrainPrayerPoints(expectedDrain);
    }

    [TestMethod]
    public void CryptRope_HandlesAllFourEntryRopes()
    {
        var ids = typeof(CryptRope).GetCustomAttributes(typeof(GameObjectScriptMetaDataAttribute), false)
            .Cast<GameObjectScriptMetaDataAttribute>().SelectMany(a => a.ObjectIds).Order().ToArray();

        CollectionAssert.AreEqual(new[] { 6709, 6710, 6711, 6712 }, ids);
    }

    [TestMethod]
    [DataRow(0, 1, 450)]
    [DataRow(1, 2, 392)]
    [DataRow(6, 7, 102)]
    [DataRow(7, 7, 73)]
    public void Rewards_RollCountAndDenominatorFollowSlainBrothers(int slain, int rolls, int denominator)
    {
        Assert.AreEqual(rolls, BarrowsRewards.RollCount(slain));
        Assert.AreEqual(denominator, BarrowsRewards.Denominator(slain));
    }

    [TestMethod]
    public void Rewards_NoSlainBrothersNeverRollsArmour()
    {
        var tables = BarrowsRewards.RollRewardTables([], 0, _ => 0);

        Assert.AreEqual(1, tables.Count);
        Assert.IsFalse(tables.Any(t => BarrowsRewards.BrotherSetLootTableIds.Contains(t)));
    }

    [TestMethod]
    public void Rewards_OnlySlainBrothersCanBeHit()
    {
        var tables = BarrowsRewards.RollRewardTables([1, 4], 0, _ => 0);

        Assert.AreEqual(BarrowsRewards.RollCount(2), tables.Count);
        Assert.IsTrue(tables.All(t => t == BarrowsRewards.BrotherSetLootTableIds[1]));
    }

    [TestMethod]
    public void Rewards_MissesFallBackToOrdinaryBands()
    {
        var tables = BarrowsRewards.RollRewardTables([0, 1, 2], 0, _ => 1);

        Assert.AreEqual(BarrowsRewards.RollCount(3), tables.Count);
        Assert.IsTrue(tables.All(t => t == BarrowsRewards.OrdinaryTableId(1)));
    }

    [TestMethod]
    [DataRow(0, 42)]
    [DataRow(379, 42)]
    [DataRow(380, 43)]
    [DataRow(504, 43)]
    [DataRow(505, 44)]
    [DataRow(629, 44)]
    [DataRow(630, 45)]
    [DataRow(754, 45)]
    [DataRow(755, 46)]
    [DataRow(879, 46)]
    [DataRow(880, 47)]
    [DataRow(1004, 47)]
    [DataRow(1005, 48)]
    [DataRow(1007, 48)]
    [DataRow(1008, 49)]
    [DataRow(1010, 49)]
    [DataRow(1011, 50)]
    [DataRow(1014, 50)]
    public void Rewards_OrdinaryBandsFollowTheRollThresholds(int roll, int tableId)
    {
        Assert.AreEqual(tableId, BarrowsRewards.OrdinaryTableId(roll));
    }

    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(300, 6, 312)]
    [DataRow(5000, 6, 1012)]
    public void Rewards_PotentialCapsCreatureLevelsThenAddsTwoPerBrother(int creatureLevels, int brothers, int expected)
    {
        Assert.AreEqual(expected, BarrowsRewards.RewardPotential(creatureLevels, brothers));
    }

    [TestMethod]
    public void Rewards_RollIsBoundedByThePotential()
    {
        var maxima = new List<int>();

        BarrowsRewards.RollRewardTables([], 100, max =>
        {
            maxima.Add(max);
            return 0;
        });

        CollectionAssert.AreEqual(new[] { 101 }, maxima);
    }

    [TestMethod]
    public void KillCredit_CryptCreaturesAddTheirCombatLevelToTheRewardPotential()
    {
        EventHappened<CreatureKillEvent>? handler = null;
        var character = Substitute.For<ICharacter>();
        character.RegisterEventHandler(Arg.Do<EventHappened<CreatureKillEvent>>(h => handler = h));
        var (profile, _) = Create(new BarrowsDto(), character: character);
        var creature = Substitute.For<INpc>();
        creature.Appearance.CompositeID.Returns(2035);

        handler!(new CreatureKillEvent(character, creature));

        profile.Received().SetObject(BarrowsConstants.MinigamesBarrows, Arg.Is<BarrowsDto>(d => d.CreatureLevels == 79 && d.KillCount == 1));
    }

    [TestMethod]
    public void LootChest_RollsArmourTablesOnlyForSlainBrothers()
    {
        var (character, lootService, _) = CreateClaimableChestCharacter();
        var (_, script) = Create(new BarrowsDto { KarilKilled = true }, character: character, lootService: lootService);

        script.LootChest(Substitute.For<IGameObject>());

        lootService.DidNotReceive().FindGameObjectLootTable(BarrowsRewards.BrotherSetLootTableIds[BarrowsConstants.AhrimIndex]);
        lootService.DidNotReceive().FindGameObjectLootTable(BarrowsRewards.BrotherSetLootTableIds[BarrowsConstants.VeracIndex]);
    }

    [TestMethod]
    public void Puzzle_LocksEveryChestDoorExceptTheOperableOne()
    {
        var character = Substitute.For<ICharacter>();
        var (_, script) = Create(new BarrowsDto { CryptStartIndex = 0, PuzzleDoorIndex = 1 }, character: character);

        script.InCrypts = true;

        var expected = 64 | (1 << 14) | (1 << 19) | (1 << 21); // slots 4, 9, 11 locked; slot 8 operable
        character.Configurations.Received().SendStandardConfiguration(452, expected);
    }

    [TestMethod]
    [DataRow(6720, 0)]
    [DataRow(6739, 0)]
    [DataRow(6724, 1)]
    [DataRow(6743, 1)]
    [DataRow(6725, 2)]
    [DataRow(6744, 2)]
    [DataRow(6727, 3)]
    [DataRow(6746, 3)]
    [DataRow(6718, -1)]
    [DataRow(6749, -1)]
    public void Puzzle_ChestDoorIndexMatchesBothDoorLeaves(int objectId, int expected)
    {
        Assert.AreEqual(expected, BarrowsPuzzle.ChestDoorIndex(objectId));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void Puzzle_ChoicesAlwaysContainTheCorrectModelAtTheReportedPosition(int puzzle)
    {
        var correct = new[] { 6713, 6719, 6725, 6731 }[puzzle];

        for (var seed = 0; seed < 20; seed++)
        {
            var random = new Random(seed);
            var (choices, correctIndex) = BarrowsPuzzle.Choices(puzzle, max => random.Next(max));

            Assert.AreEqual(correct, choices[correctIndex]);
            Assert.AreEqual(3, choices.Distinct().Count());
        }
    }

    [TestMethod]
    public void Puzzle_NewRunCanStartWithAnyOfTheFourDoors()
    {
        var doors = Enumerable.Range(0, 4).Select(pick => BarrowsPuzzle.New(max => Math.Min(pick, max - 1)).DoorIndex);

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, doors.ToArray());
    }

    [TestMethod]
    public void Puzzle_RerollAlwaysMovesToADifferentDoor()
    {
        for (var door = 0; door < 4; door++)
        {
            for (var pick = 0; pick < 3; pick++)
            {
                var values = new Queue<int>([pick, 2]);
                var (newDoor, puzzle) = BarrowsPuzzle.Reroll(door, _ => values.Dequeue());

                Assert.AreNotEqual(door, newDoor);
                Assert.AreEqual(2, puzzle);
            }
        }
    }

    [TestMethod]
    public void TryPassChestDoor_LockedDoorStaysClosedWithoutOpeningThePuzzle()
    {
        var character = Substitute.For<ICharacter>();
        var (_, script) = Create(new BarrowsDto { PuzzleDoorIndex = 0 }, character: character);

        var passed = script.TryPassChestDoor(6724, () => Assert.Fail("must not run"));

        Assert.IsFalse(passed);
        character.Widgets.DidNotReceive().OpenWidget(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IWidgetScript>(), Arg.Any<bool>());
    }

    [TestMethod]
    public void TryPassChestDoor_OperableDoorOpensThePuzzleAndOnlyACorrectAnswerPasses()
    {
        var character = Substitute.For<ICharacter>();
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(BarrowsPuzzleWidget)).Returns(new BarrowsPuzzleWidget(Substitute.For<ICharacterContextAccessor>()));
        character.ServiceProvider.Returns(services);
        var (profile, script) = Create(new BarrowsDto { PuzzleDoorIndex = 2, PuzzleIndex = 1 }, character: character);
        var passed = 0;

        Assert.IsFalse(script.TryPassChestDoor(6725, () => passed++));
        character.Widgets.Received(1).OpenWidget(BarrowsPuzzle.InterfaceId, 0, Arg.Any<IWidgetScript>(), false);
        script.AnswerPuzzle(false);

        Assert.AreEqual(0, passed);
        profile.Received().SetObject(BarrowsConstants.MinigamesBarrows,
            Arg.Is<BarrowsDto>(d => !d.PuzzleSolved && d.PuzzleDoorIndex != 2 && d.CryptStartIndex != 0));

        Assert.IsFalse(script.TryPassChestDoor(6725, () => passed++));
    }

    [TestMethod]
    public void AnswerPuzzle_CorrectAnswerUnlocksTheDoorsAndPasses()
    {
        var character = Substitute.For<ICharacter>();
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(BarrowsPuzzleWidget)).Returns(new BarrowsPuzzleWidget(Substitute.For<ICharacterContextAccessor>()));
        character.ServiceProvider.Returns(services);
        var (profile, script) = Create(new BarrowsDto { CryptStartIndex = 0, PuzzleDoorIndex = 0 }, character: character);
        var passed = 0;
        script.TryPassChestDoor(6720, () => passed++);

        script.AnswerPuzzle(true);

        Assert.AreEqual(1, passed);
        profile.Received().SetObject(BarrowsConstants.MinigamesBarrows, Arg.Is<BarrowsDto>(d => d.PuzzleSolved));
        character.Configurations.Received().SendStandardConfiguration(452, 64);
        Assert.IsTrue(script.TryPassChestDoor(6724, () => Assert.Fail("solved doors pass directly")));
    }

    [TestMethod]
    public void OnRemove_ClearsTheChestOpenStateSoTheNextRunMeetsTheTunnelBrother()
    {
        var character = Substitute.For<ICharacter>();
        var (_, script) = Create(new BarrowsDto { LootedChest = true }, character: character);

        script.OnRemove();

        character.Received().RemoveState<BarrowsOpenedChestState>();
        character.Configurations.Received().SendStandardConfiguration(453, 0);
    }

    [TestMethod]
    public void OnRemove_AfterClaimKeepsTheTunnelRolledAtTheSearch()
    {
        var (profile, script) = Create(new BarrowsDto { LootedChest = true, TunnelIndex = 3 });
        profile.ClearReceivedCalls();

        script.OnRemove();

        profile.Received(1).SetObject(BarrowsConstants.MinigamesBarrows, Arg.Is<BarrowsDto>(d => d.TunnelIndex == 3 && !d.LootedChest));
    }

    [TestMethod]
    public void Construction_NormalizesLegacyTunnelIndexSixAndKeepsOtherProgress()
    {
        var (npcBuilder, hintIconBuilder, npcId, _, _) = CreateSpawnableBuilders();
        var (profile, script) = Create(new BarrowsDto { TunnelIndex = 6, AhrimKilled = true, KillCount = 3 }, npcBuilder, hintIconBuilder);

        script.OpenChest();
        script.CryptGameObjectClickPerformed();

        Assert.IsTrue(script.IsKilled(BarrowsConstants.AhrimIndex));
        profile.Received().SetObject(BarrowsConstants.MinigamesBarrows,
            Arg.Is<BarrowsDto>(d => d.TunnelIndex >= 0 && d.TunnelIndex < BarrowsConstants.MoundCount && d.AhrimKilled && d.KillCount == 3));
        npcId.DidNotReceive().WithId(BarrowsConstants.BarrowBrotherIDs[BarrowsConstants.AkrisaeIndex]);
    }

    [TestMethod]
    public void OnRemove_AfterClaimStartsAFreshPuzzle()
    {
        var (profile, script) = Create(new BarrowsDto { LootedChest = true, PuzzleSolved = true });
        profile.ClearReceivedCalls();

        script.OnRemove();

        profile.Received(1).SetObject(BarrowsConstants.MinigamesBarrows,
            Arg.Is<BarrowsDto>(d => !d.PuzzleSolved && d.PuzzleDoorIndex >= 0 && d.PuzzleDoorIndex < 4 && d.PuzzleIndex >= 0 && d.PuzzleIndex < 4));
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
    [DataRow(false)]
    [DataRow(true)]
    public void OnRemove_AlwaysSendsPowerDrainsMessage(bool claimed)
    {
        var character = Substitute.For<ICharacter>();
        var (_, script) = Create(new BarrowsDto { LootedChest = claimed }, character: character);

        script.OnRemove();

        character.Received(1).SendChatMessage("The power of all those you slew in the barrows drains from your body.");
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

    private static (ICharacter Character, ILootService LootService, ILootGenerator Generator) CreateClaimableChestCharacter()
    {
        var generator = Substitute.For<ILootGenerator>();
        generator.GenerateLoot<ILootItem>(Arg.Any<LootParams>()).Returns(new List<LootResult<ILootItem>>());
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ILootGenerator)).Returns(generator);
        services.GetService(typeof(IItemBuilder)).Returns(Substitute.For<IItemBuilder>());
        services.GetService(typeof(IGroundItemBuilder)).Returns(Substitute.For<IGroundItemBuilder>());
        var character = Substitute.For<ICharacter>();
        character.ServiceProvider.Returns(services);
        character.HasState<BarrowsOpenedChestState>().Returns(true);
        var lootService = Substitute.For<ILootService>();
        lootService.FindGameObjectLootTable(Arg.Any<int>()).Returns(Substitute.For<ILootTable>());
        return (character, lootService, generator);
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
        ICharacter? character = null, ILootService? lootService = null)
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

        var script = new BarrowsScript(accessor, Substitute.For<IItemService>(), lootService ?? Substitute.For<ILootService>(),
            npcBuilder ?? Substitute.For<INpcBuilder>(), hintIconBuilder ?? Substitute.For<IHintIconBuilder>());
        return (profile, script);
    }
}
