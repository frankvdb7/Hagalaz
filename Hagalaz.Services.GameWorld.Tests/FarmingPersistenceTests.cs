using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using System.Threading;
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
    public void GetDiseaseChanceDenominator_WateredInterval_UsesProtectedDenominator()
    {
        Assert.AreEqual(25, FarmingPatchTickTask.GetDiseaseChanceDenominator(PatchCondition.Planted));
        Assert.AreEqual(35, FarmingPatchTickTask.GetDiseaseChanceDenominator(PatchCondition.Planted | PatchCondition.Watered));
        Assert.AreEqual(45, FarmingPatchTickTask.GetDiseaseChanceDenominator(PatchCondition.Planted | PatchCondition.Watered | PatchCondition.Fertilized));
        Assert.AreEqual(55, FarmingPatchTickTask.GetDiseaseChanceDenominator(PatchCondition.Planted | PatchCondition.Watered | PatchCondition.SuperFertilized));
    }

    [TestMethod]
    public void Hydrate_OfflineCycleCompletesWhileWatered_ConsumesWateredForNextCycle()
    {
        var farming = CreateFarming([CreatePatchDefinition(8389)], [CreateSeed(5374, maxCycles: 10)], offlineTicks: 100);

        farming.Hydrate(new HydratedFarmingDto
        {
            Patches =
            [
                new HydratedFarmingDto.PatchDto
                {
                    Id = 8389,
                    SeedId = 5374,
                    Condition = PatchCondition.Planted | PatchCondition.Watered,
                    CurrentCycle = 1,
                    CurrentCycleTicks = 0,
                    ProductCount = 0
                }
            ]
        });

        var dehydrated = farming.Dehydrate().Patches.Single();

        Assert.AreEqual(2, dehydrated.CurrentCycle);
        Assert.IsFalse(dehydrated.Condition.HasFlag(PatchCondition.Watered));
    }

    [TestMethod]
    public void Hydrate_WateredCycleCompletes_DiseaseRollUsesWateredChance()
    {
        // RandomStatic is not seedable, so compare disease frequency over many rolls:
        // watered 1/35 (~1143 of 40000) versus 1/25 (~1600) if the flag were cleared first.
        const int trials = 40_000;
        var now = DateTimeOffset.Now;
        var task = CreatePatchTask(8389, CreateSeed(5374, maxCycles: 10), now.AddMilliseconds(-(100 * 600 + 300)));
        var diseased = 0;

        for (var i = 0; i < trials; i++)
        {
            task.Hydrate(new HydratedFarmingDto.PatchDto
            {
                Id = 8389,
                SeedId = 5374,
                Condition = PatchCondition.Planted | PatchCondition.Watered,
                CurrentCycle = 1,
                CurrentCycleTicks = 0,
                ProductCount = 0
            }, now);

            Assert.IsFalse(task.HasCondition(PatchCondition.Watered));
            if (task.HasCondition(PatchCondition.Diseased))
                diseased++;
        }

        Assert.IsLessThan(1370, diseased);
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

    private static FarmingPatchTickTask CreateMaturePatch(
        PatchType type,
        PatchCondition extraConditions,
        int productCount,
        int maximumProductCount = 6,
        int cycleTicks = 100,
        int offlineTicks = 0,
        int savedProgress = 0)
    {
        var now = new DateTimeOffset(2026, 9, 23, 20, 0, 0, TimeSpan.Zero);
        var seed = CreateSeed(5374, cycleTicks: cycleTicks, maxCycles: 4, type: type, minimumProductCount: 1, maximumProductCount: maximumProductCount);
        var task = CreatePatchTask(8389, seed, now.AddMilliseconds(-(offlineTicks * 600L)));
        task.Hydrate(new HydratedFarmingDto.PatchDto
        {
            Id = 8389,
            SeedId = 5374,
            Condition = PatchCondition.Planted | PatchCondition.Mature | PatchCondition.Cleared | extraConditions,
            CurrentCycle = 4,
            CurrentCycleTicks = savedProgress,
            ProductCount = productCount
        }, now);
        return task;
    }

    [TestMethod]
    public void FellTree_CheckedMatureTree_LeavesStumpOnceAndClearEmptiesPatch()
    {
        var task = CreateMaturePatch(PatchType.Tree, PatchCondition.Checked, productCount: 1);

        Assert.IsTrue(task.FellTree());
        Assert.IsTrue(task.HasCondition(PatchCondition.Empty));
        Assert.IsTrue(task.HasCondition(PatchCondition.Planted));
        Assert.AreEqual(0, task.ProductCount);
        Assert.IsFalse(task.FellTree());

        task.Clear();

        Assert.IsFalse(task.HasCondition(PatchCondition.Planted));
        Assert.IsFalse(task.HasCondition(PatchCondition.Checked));
        Assert.IsFalse(task.HasCondition(PatchCondition.Empty));
    }

    [TestMethod]
    public void FellTree_UncheckedTreeOrOtherCrop_IsRejected()
    {
        var unchecked_ = CreateMaturePatch(PatchType.Tree, 0, productCount: 1);
        var fruitTree = CreateMaturePatch(PatchType.FruitTree, PatchCondition.Checked, productCount: 3);

        Assert.IsFalse(unchecked_.FellTree());
        Assert.IsFalse(fruitTree.FellTree());
        Assert.IsFalse(fruitTree.HasCondition(PatchCondition.Empty));
        Assert.AreEqual(3, fruitTree.ProductCount);
    }

    [TestMethod]
    public void Clear_AfterCheckedTree_RequiresANewHealthCheckForTheNextCrop()
    {
        var task = CreateMaturePatch(PatchType.Tree, PatchCondition.Checked, productCount: 1);

        task.Clear();
        task.Plant(CreateSeed(5374, type: PatchType.Tree));
        task.AddCondition(PatchCondition.Mature);

        Assert.IsFalse(task.HasCondition(PatchCondition.Checked));
    }

    [TestMethod]
    public void Harvest_FruitTreeEmptied_KeepsMaturePlantInPatch()
    {
        var task = CreateMaturePatch(PatchType.FruitTree, PatchCondition.Checked, productCount: 2);

        task.Harvest();
        task.Harvest();

        Assert.AreEqual(0, task.ProductCount);
        Assert.IsTrue(task.HasCondition(PatchCondition.Planted));
        Assert.IsTrue(task.HasCondition(PatchCondition.Mature));
        Assert.IsTrue(task.HasCondition(PatchCondition.Checked));

        task.Harvest();
        Assert.AreEqual(0, task.ProductCount);
    }

    [TestMethod]
    public void Harvest_AllotmentEmptied_ClearsPatch()
    {
        var task = CreateMaturePatch(PatchType.Allotment, 0, productCount: 1);

        task.Harvest();

        Assert.IsFalse(task.HasCondition(PatchCondition.Planted));
        Assert.IsTrue(task.HasCondition(PatchCondition.Cleared));
    }

    [TestMethod]
    public void Tick_EmptiedBush_RegrowsOneBerryPerCycleUpToTheMaximum()
    {
        var task = CreateMaturePatch(PatchType.Bush, PatchCondition.Checked, productCount: 1, maximumProductCount: 2, cycleTicks: 10);
        task.Harvest();
        Assert.AreEqual(0, task.ProductCount);

        for (var i = 0; i < 10; i++)
            task.Tick();
        Assert.AreEqual(1, task.ProductCount);

        for (var i = 0; i < 30; i++)
            task.Tick();
        Assert.AreEqual(2, task.ProductCount);
        Assert.AreEqual(0, task.TickCount);
    }

    [TestMethod]
    public void Hydrate_MatureFruitTree_ReplenishesElapsedCyclesWithoutExceedingMaximum()
    {
        var partial = CreateMaturePatch(PatchType.FruitTree, PatchCondition.Checked, productCount: 1, cycleTicks: 100, offlineTicks: 250);
        var capped = CreateMaturePatch(PatchType.FruitTree, PatchCondition.Checked, productCount: 1, maximumProductCount: 3, cycleTicks: 100, offlineTicks: 100_000);

        Assert.AreEqual(3, partial.ProductCount);
        Assert.AreEqual(50, partial.TickCount);
        Assert.AreEqual(3, capped.ProductCount);
        Assert.AreEqual(0, capped.TickCount);
        Assert.AreEqual(3, capped.Dehydrate().ProductCount);
    }

    [TestMethod]
    public void Tick_MatureFruitTree_RegrowsOnTheRegrowIntervalNotTheGrowthCycle()
    {
        var now = new DateTimeOffset(2026, 9, 23, 20, 0, 0, TimeSpan.Zero);
        var seed = CreateSeed(5283, cycleTicks: 16000, maxCycles: 6, type: PatchType.FruitTree, minimumProductCount: 6, maximumProductCount: 6, productRegrowTicks: 20);
        var task = CreatePatchTask(8389, seed, now);
        task.Hydrate(new HydratedFarmingDto.PatchDto
        {
            Id = 8389, SeedId = 5283, Condition = PatchCondition.Planted | PatchCondition.Mature | PatchCondition.Checked,
            CurrentCycle = 6, CurrentCycleTicks = 0, ProductCount = 6
        }, now);
        task.Harvest();
        task.Harvest();

        for (var i = 0; i < 20; i++)
            task.Tick();

        Assert.AreEqual(5, task.ProductCount);
    }

    [TestMethod]
    [DataRow(PatchCondition.Planted, 0, 3, 8 + 3)]
    [DataRow(PatchCondition.Planted | PatchCondition.Mature, 6, 6, 8 + 26)]
    [DataRow(PatchCondition.Planted | PatchCondition.Mature | PatchCondition.Checked, 4, 6, 8 + 6 + 4)]
    [DataRow(PatchCondition.Planted | PatchCondition.Diseased, 0, 2, 8 + 13 + 2)]
    [DataRow(PatchCondition.Planted | PatchCondition.Dead, 0, 2, 8 + 19 + 2)]
    public void Refresh_AppleTree_SendsCacheVarbitStateForPlantStage(PatchCondition condition, int productCount, int cycle, int expected)
    {
        var task = CreateFruitTreeRefreshTask(slot: 0, out var configurations);
        task.Hydrate(new HydratedFarmingDto.PatchDto
        {
            Id = 7965, SeedId = 5283, Condition = condition, CurrentCycle = cycle, CurrentCycleTicks = 0, ProductCount = productCount
        }, DateTimeOffset.Now);

        task.Refresh();

        configurations.Received(1).SendBitConfiguration(707, expected);
    }

    [TestMethod]
    [DataRow(1, 35)]
    [DataRow(2, 72)]
    [DataRow(3, 99)]
    [DataRow(4, 136)]
    [DataRow(5, 163)]
    [DataRow(6, 200)]
    public void Refresh_FruitTreeSpecies_StartsAtItsCacheVarbitBase(int slot, int expectedBase)
    {
        var task = CreateFruitTreeRefreshTask(slot, out var configurations);
        task.Hydrate(new HydratedFarmingDto.PatchDto
        {
            Id = 7965, SeedId = 5283, Condition = PatchCondition.Planted, CurrentCycle = 0, CurrentCycleTicks = 0, ProductCount = 0
        }, DateTimeOffset.Now);

        task.Refresh();

        configurations.Received(1).SendBitConfiguration(707, expectedBase);
    }

    [TestMethod]
    [DataRow(0, 5, PatchCondition.Planted, 0, 3, 5 + 3)]
    [DataRow(0, 5, PatchCondition.Planted | PatchCondition.Mature, 4, 5, 250)]
    [DataRow(0, 5, PatchCondition.Planted | PatchCondition.Mature | PatchCondition.Checked, 4, 5, 5 + 5 + 4)]
    [DataRow(0, 5, PatchCondition.Planted | PatchCondition.Diseased, 0, 2, 70 + 2)]
    [DataRow(0, 5, PatchCondition.Planted | PatchCondition.Dead, 0, 1, 134 + 1)]
    [DataRow(5, 8, PatchCondition.Planted | PatchCondition.Mature, 4, 8, 255)]
    [DataRow(5, 8, PatchCondition.Planted | PatchCondition.Mature | PatchCondition.Checked, 2, 8, 197 + 8 + 2)]
    public void Refresh_Bush_SendsCacheVarbitStateForPlantStage(int slot, int maxCycles, PatchCondition condition, int productCount, int cycle, int expected)
    {
        var task = CreateFruitTreeRefreshTask(slot, out var configurations, PatchType.Bush, maxCycles);
        task.Hydrate(new HydratedFarmingDto.PatchDto
        {
            Id = 7965, SeedId = 5283, Condition = condition, CurrentCycle = cycle, CurrentCycleTicks = 0, ProductCount = productCount
        }, DateTimeOffset.Now);

        task.Refresh();

        configurations.Received(1).SendBitConfiguration(707, expected);
    }

    private static FarmingPatchTickTask CreateFruitTreeRefreshTask(int slot, out IConfigurations configurations, PatchType type = PatchType.FruitTree, int maxCycles = 6)
    {
        var seed = CreateSeed(5283, cycleTicks: 16000, maxCycles: maxCycles, type: type, minimumProductCount: 4, maximumProductCount: 6, varpBitIndex: slot);
        var farmingService = Substitute.For<IFarmingService>();
        farmingService.FindPatchById(7965).Returns(Task.FromResult<PatchDto?>(CreatePatchDefinition(7965, type)));
        farmingService.FindSeedById(5283).Returns(Task.FromResult<SeedDto?>(seed));
        var objectService = Substitute.For<IGameObjectService>();
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.VarpBitFileId.Returns(707);
        objectService.FindGameObjectDefinitionById(7965).Returns(Task.FromResult(definition));
        var owner = Substitute.For<ICharacter>();
        owner.LastLogin.Returns(DateTimeOffset.Now);
        configurations = Substitute.For<IConfigurations>();
        owner.Configurations.Returns(configurations);
        owner.QueueTask(Arg.Any<Func<CancellationToken, Task>>()).Returns(call =>
        {
            call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None).GetAwaiter().GetResult();
            return Substitute.For<IRsTaskHandle>();
        });
        return new FarmingPatchTickTask(owner, 7965, farmingService, objectService);
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
        int maximumProductCount = 3,
        int productRegrowTicks = 0,
        int varpBitIndex = 1) => new()
    {
        ItemID = itemId,
        ProductID = 100,
        MinimumProductCount = minimumProductCount,
        MaximumProductCount = maximumProductCount,
        RequiredLevel = 1,
        PlantingExperience = 0,
        HarvestExperience = 0,
        VarpBitIndex = varpBitIndex,
        ProductRegrowTicks = productRegrowTicks,
        MaxCycles = maxCycles,
        CycleTicks = cycleTicks,
        Type = type
    };
}
