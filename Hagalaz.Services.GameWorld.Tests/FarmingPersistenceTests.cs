using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Logic.Characters;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class FarmingPersistenceTests
{
    [TestMethod]
    public void HydrateDehydrate_MultiplePatches_PreservesPatchAndSeedIdentityAndState()
    {
        var patchDefinitions = new[] { CreatePatchDefinition(8389), CreatePatchDefinition(8550) };
        var seeds = new[] { CreateSeed(5374), CreateSeed(5318) };
        var farming = CreateFarming(patchDefinitions, seeds);

        farming.Hydrate(new HydratedFarmingDto
        {
            Patches =
            [
                new HydratedFarmingDto.PatchDto
                {
                    Id = 8389,
                    SeedId = 5374,
                    Condition = PatchCondition.Cleared | PatchCondition.Fertilized,
                    CurrentCycle = 3,
                    CurrentCycleTicks = 0,
                    ProductCount = 2
                },
                new HydratedFarmingDto.PatchDto
                {
                    Id = 8550,
                    SeedId = 5318,
                    Condition = PatchCondition.Planted | PatchCondition.Watered,
                    CurrentCycle = 1,
                    CurrentCycleTicks = 0,
                    ProductCount = 3
                }
            ]
        });

        Assert.IsNull(farming.GetFarmingPatch(8389)!.Seed);
        Assert.AreEqual(5318, farming.GetFarmingPatch(8550)!.Seed!.ItemID);

        var patches = farming.Dehydrate().Patches.ToDictionary(patch => patch.Id);

        Assert.AreEqual(2, patches.Count);
        Assert.IsTrue(patches.ContainsKey(8389));
        Assert.IsTrue(patches.ContainsKey(8550));
        Assert.AreEqual(5374, patches[8389].SeedId);
        Assert.AreEqual(5318, patches[8550].SeedId);
        Assert.AreEqual(PatchCondition.Cleared | PatchCondition.Fertilized, patches[8389].Condition);
        Assert.AreEqual(PatchCondition.Planted | PatchCondition.Watered, patches[8550].Condition);
        Assert.AreEqual(2, patches[8389].ProductCount);
        Assert.AreEqual(3, patches[8550].ProductCount);
        Assert.AreEqual(3, patches[8389].CurrentCycle);
        Assert.AreEqual(1, patches[8550].CurrentCycle);
        Assert.AreEqual(0, patches[8389].CurrentCycleTicks);
        Assert.AreEqual(0, patches[8550].CurrentCycleTicks);
    }

    [TestMethod]
    public void Clear_UnplantedHydratedPatch_RetainsSeedIdentityAndCollectionMembership()
    {
        var farming = CreateFarming([CreatePatchDefinition(8389)], [CreateSeed(5374)]);
        farming.Hydrate(new HydratedFarmingDto
        {
            Patches =
            [
                new HydratedFarmingDto.PatchDto
                {
                    Id = 8389,
                    SeedId = 5374,
                    Condition = PatchCondition.Cleared,
                    CurrentCycle = 2,
                    CurrentCycleTicks = 0,
                    ProductCount = 0
                }
            ]
        });

        var patch = farming.GetFarmingPatch(8389)!;
        patch.Clear();

        Assert.IsFalse(patch.HasCondition(PatchCondition.Planted));
        Assert.IsNull(patch.Seed);
        Assert.AreSame(patch, farming.GetFarmingPatch(8389));
        var dehydrated = farming.Dehydrate().Patches.Single();
        Assert.AreEqual(8389, dehydrated.Id);
        Assert.AreEqual(5374, dehydrated.SeedId);
    }

    [TestMethod]
    public void Plant_NewSeed_UpdatesRetainedSeedIdentity()
    {
        var firstSeed = CreateSeed(5374);
        var replacementSeed = CreateSeed(5318);
        var farming = CreateFarming([CreatePatchDefinition(8389)], [firstSeed, replacementSeed]);
        farming.Hydrate(new HydratedFarmingDto
        {
            Patches =
            [
                new HydratedFarmingDto.PatchDto
                {
                    Id = 8389,
                    SeedId = firstSeed.ItemID,
                    Condition = PatchCondition.Cleared,
                    CurrentCycle = 3,
                    CurrentCycleTicks = 0,
                    ProductCount = 0
                }
            ]
        });

        var patch = farming.GetFarmingPatch(8389)!;
        patch.Plant(replacementSeed);

        Assert.AreSame(replacementSeed, patch.Seed);
        Assert.AreEqual(5318, farming.Dehydrate().Patches.Single().SeedId);
    }

    [TestMethod]
    public void Hydrate_NoOfflineTicks_PreservesSavedCycleProgress()
    {
        const int savedProgress = 24;
        var farming = CreateFarming([CreatePatchDefinition(8389)], [CreateSeed(5374)], offlineTicks: 0);

        farming.Hydrate(new HydratedFarmingDto
        {
            Patches =
            [
                new HydratedFarmingDto.PatchDto
                {
                    Id = 8389,
                    SeedId = 5374,
                    Condition = PatchCondition.Planted,
                    CurrentCycle = 1,
                    CurrentCycleTicks = savedProgress,
                    ProductCount = 0
                }
            ]
        });

        var dehydrated = farming.Dehydrate().Patches.Single();

        Assert.AreEqual(savedProgress, dehydrated.CurrentCycleTicks);
        Assert.AreEqual(1, dehydrated.CurrentCycle);
        Assert.AreEqual(PatchCondition.Planted, dehydrated.Condition);
    }

    [TestMethod]
    public void Hydrate_PartialOfflineTicks_AddsElapsedProgressWithoutCompletingCycle()
    {
        const int savedProgress = 24;
        const int offlineTicks = 10;
        var farming = CreateFarming([CreatePatchDefinition(8389)], [CreateSeed(5374)], offlineTicks);

        farming.Hydrate(new HydratedFarmingDto
        {
            Patches =
            [
                new HydratedFarmingDto.PatchDto
                {
                    Id = 8389,
                    SeedId = 5374,
                    Condition = PatchCondition.Planted,
                    CurrentCycle = 1,
                    CurrentCycleTicks = savedProgress,
                    ProductCount = 0
                }
            ]
        });

        var dehydrated = farming.Dehydrate().Patches.Single();

        Assert.AreEqual(8389, dehydrated.Id);
        Assert.AreEqual(5374, dehydrated.SeedId);
        Assert.AreEqual(1, dehydrated.CurrentCycle);
        Assert.AreEqual(PatchCondition.Planted, dehydrated.Condition);
        Assert.AreEqual(0, dehydrated.ProductCount);
        Assert.AreEqual(savedProgress + offlineTicks, dehydrated.CurrentCycleTicks);
    }

    [TestMethod]
    public void Hydrate_OfflineTicksCompleteOneCycle_AdvancesCycleAndResetsProgress()
    {
        var farming = CreateFarming([CreatePatchDefinition(8389)], [CreateSeed(5374)], offlineTicks: 10);

        farming.Hydrate(new HydratedFarmingDto
        {
            Patches =
            [
                new HydratedFarmingDto.PatchDto
                {
                    Id = 8389,
                    SeedId = 5374,
                    Condition = PatchCondition.Planted,
                    CurrentCycle = 1,
                    CurrentCycleTicks = 90,
                    ProductCount = 0
                }
            ]
        });

        var dehydrated = farming.Dehydrate().Patches.Single();

        Assert.AreEqual(2, dehydrated.CurrentCycle);
        Assert.IsTrue(dehydrated.Condition.HasFlag(PatchCondition.Planted));
        Assert.AreEqual(0, dehydrated.CurrentCycleTicks);
        Assert.AreEqual(0, dehydrated.ProductCount);
    }

    [TestMethod]
    public void Hydrate_OfflineTicksCrossMultipleCycles_PreservesGrowthAndDiseaseSemantics()
    {
        var farming = CreateFarming([CreatePatchDefinition(8389)], [CreateSeed(5374)], offlineTicks: 250);

        farming.Hydrate(new HydratedFarmingDto
        {
            Patches =
            [
                new HydratedFarmingDto.PatchDto
                {
                    Id = 8389,
                    SeedId = 5374,
                    Condition = PatchCondition.Planted,
                    CurrentCycle = 1,
                    CurrentCycleTicks = 50,
                    ProductCount = 0
                }
            ]
        });

        var dehydrated = farming.Dehydrate().Patches.Single();

        Assert.IsTrue(dehydrated.Condition.HasFlag(PatchCondition.Planted));
        Assert.AreEqual(0, dehydrated.CurrentCycleTicks);

        // Grow(false) uses RandomStatic.Generator for disease checks before maturity.
        // Either no disease is rolled and all three cycles reach maturity, or a
        // disease roll is followed by the existing death transition on the next cycle.
        if (dehydrated.Condition.HasFlag(PatchCondition.Mature))
        {
            Assert.AreEqual(4, dehydrated.CurrentCycle);
            Assert.IsFalse(dehydrated.Condition.HasFlag(PatchCondition.Dead));
            Assert.IsInRange(1, 3, dehydrated.ProductCount);
        }
        else
        {
            Assert.IsTrue(dehydrated.Condition.HasFlag(PatchCondition.Dead));
            Assert.IsFalse(dehydrated.Condition.HasFlag(PatchCondition.Mature));
            Assert.IsTrue(dehydrated.CurrentCycle is 2 or 3);
            Assert.AreEqual(0, dehydrated.ProductCount);
        }
    }

    [TestMethod]
    public void Hydrate_LargeLegacyProgressBelowIntMax_NormalizesWithinActiveCycle()
    {
        const int savedProgress = int.MaxValue - 1;
        var now = new DateTimeOffset(2026, 9, 23, 20, 52, 51, TimeSpan.Zero);
        var task = CreatePatchTask(
            8389,
            CreateSeed(5374, cycleTicks: 3500, maxCycles: 12, type: PatchType.Tree),
            now);

        task.Hydrate(new HydratedFarmingDto.PatchDto
        {
            Id = 8389,
            SeedId = 5374,
            Condition = PatchCondition.Planted,
            CurrentCycle = 1,
            CurrentCycleTicks = savedProgress,
            ProductCount = 0
        }, now);

        Assert.AreEqual(1, task.CurrentCycle);
        Assert.AreEqual(savedProgress % 3500, task.TickCount);
        Assert.IsTrue(task.TickCount >= 0 && task.TickCount < 3500);
        Assert.AreEqual(task.TickCount, task.Dehydrate().CurrentCycleTicks);
    }

    [TestMethod]
    public void Hydrate_ObservedLegacyProgressAndFiveYearOfflineInterval_PreservesDiseaseDeathWithoutOverflow()
    {
        var lastLogin = new DateTimeOffset(2021, 1, 31, 0, 42, 1, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 9, 23, 20, 52, 51, TimeSpan.Zero).AddMilliseconds(78);
        const int savedProgress = 1_860_765_382;
        var offlineTicks = (now - lastLogin).Ticks / (TimeSpan.TicksPerMillisecond * 600L);
        Assert.IsTrue((long)savedProgress + offlineTicks > int.MaxValue);

        var task = CreatePatchTask(
            8389,
            CreateSeed(5374, cycleTicks: 3500, maxCycles: 12, type: PatchType.Tree, minimumProductCount: 1, maximumProductCount: 1),
            lastLogin);

        task.Hydrate(new HydratedFarmingDto.PatchDto
        {
            Id = 8389,
            SeedId = 5374,
            Condition = PatchCondition.Planted | PatchCondition.Diseased,
            CurrentCycle = 1,
            CurrentCycleTicks = savedProgress,
            ProductCount = 0
        }, now);

        Assert.AreEqual(1, task.CurrentCycle);
        Assert.IsTrue(task.HasCondition(PatchCondition.Dead));
        Assert.IsFalse(task.HasCondition(PatchCondition.Mature));
        Assert.AreEqual(0, task.TickCount);
        var dehydrated = task.Dehydrate();
        Assert.AreEqual(0, dehydrated.CurrentCycleTicks);
        Assert.AreEqual(8389, dehydrated.Id);
        Assert.AreEqual(5374, dehydrated.SeedId);
    }

    [TestMethod]
    public void Hydrate_ObservedMatureLegacyProgress_ResetsTerminalTicksWithoutReplayingCycles()
    {
        var lastLogin = new DateTimeOffset(2021, 1, 31, 0, 42, 1, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 9, 23, 20, 52, 51, TimeSpan.Zero).AddMilliseconds(78);
        var task = CreatePatchTask(
            8389,
            CreateSeed(5374, cycleTicks: 3500, maxCycles: 12, type: PatchType.Tree, minimumProductCount: 1, maximumProductCount: 1),
            lastLogin);

        task.Hydrate(new HydratedFarmingDto.PatchDto
        {
            Id = 8389,
            SeedId = 5374,
            Condition = PatchCondition.Planted | PatchCondition.Mature | PatchCondition.Cleared | PatchCondition.Checked,
            CurrentCycle = 12,
            CurrentCycleTicks = 1_860_765_382,
            ProductCount = 1
        }, now);

        Assert.AreEqual(12, task.CurrentCycle);
        Assert.IsTrue(task.HasCondition(PatchCondition.Mature));
        Assert.IsTrue(task.HasCondition(PatchCondition.Planted));
        Assert.AreEqual(1, task.Dehydrate().ProductCount);
        Assert.AreEqual(0, task.TickCount);
        Assert.AreEqual(0, task.Dehydrate().CurrentCycleTicks);
    }

    private static Farming CreateFarming(
        IReadOnlyList<PatchDto> patchDefinitions,
        IReadOnlyList<SeedDto> seeds,
        int offlineTicks = 0)
    {
        var definitionsById = patchDefinitions.ToDictionary(definition => definition.ObjectID);
        var seedsById = seeds.ToDictionary(seed => seed.ItemID);
        var farmingService = Substitute.For<IFarmingService>();
        farmingService.FindPatchById(Arg.Any<int>())
            .Returns(call => Task.FromResult(definitionsById.GetValueOrDefault(call.Arg<int>())));
        farmingService.FindSeedById(Arg.Any<int>())
            .Returns(call => Task.FromResult(seedsById.GetValueOrDefault(call.Arg<int>())));

        var owner = Substitute.For<Hagalaz.Game.Abstractions.Model.Creatures.Characters.ICharacter>();
        owner.LastLogin.Returns(_ => DateTimeOffset.Now.AddMilliseconds(-(offlineTicks * 600 + 300)));
        var farming = new Farming(owner, farmingService, Substitute.For<IGameObjectService>());
        owner.Farming.Returns(farming);
        return farming;
    }

    private static FarmingPatchTickTask CreatePatchTask(int patchId, SeedDto seed, DateTimeOffset lastLogin)
    {
        var patchDefinition = CreatePatchDefinition(patchId, seed.Type);
        var farmingService = Substitute.For<IFarmingService>();
        farmingService.FindPatchById(patchId).Returns(Task.FromResult<PatchDto?>(patchDefinition));
        farmingService.FindSeedById(seed.ItemID).Returns(Task.FromResult<SeedDto?>(seed));
        var owner = Substitute.For<ICharacter>();
        owner.LastLogin.Returns(lastLogin);
        return new FarmingPatchTickTask(owner, patchId, farmingService, Substitute.For<IGameObjectService>());
    }

    private static PatchDto CreatePatchDefinition(int objectId, PatchType type = PatchType.Allotment) => new()
    {
        ObjectID = objectId,
        Type = type
    };

    private static SeedDto CreateSeed(
        int itemId,
        int cycleTicks = 100,
        int maxCycles = 4,
        PatchType type = PatchType.Allotment,
        int minimumProductCount = 1,
        int maximumProductCount = 3) => new()
    {
        ItemID = itemId,
        ProductID = 100,
        MinimumProductCount = minimumProductCount,
        MaximumProductCount = maximumProductCount,
        RequiredLevel = 1,
        PlantingExperience = 0,
        HarvestExperience = 0,
        VarpBitIndex = 1,
        MaxCycles = maxCycles,
        CycleTicks = cycleTicks,
        Type = type
    };
}
