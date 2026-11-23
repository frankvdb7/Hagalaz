using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Scripts.Skills.Farming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hagalaz.Game.Scripts.Tests.Skills.Farming
{
    [TestClass]
    public class FarmingSkillServiceTests
    {
        // A representative non-zero value only; real 742 per-product XP must come from verified seed data.
        private const double RepresentativeProductExperience = 8.5;

        private sealed record Scenario(
            ICharacter Character,
            IFarmingPatch Patch,
            IGameObject Obj,
            List<RsTickTask> Queued,
            EventHappened Handle,
            List<EventHappened<CreatureInterruptedEvent>> Registered);

        private static Scenario CreateCheckedPlant(PatchType type, int productCount)
        {
            var remaining = productCount;
            var patch = Substitute.For<IFarmingPatch>();
            patch.PatchDefinition.Returns(new PatchDto { ObjectID = 7, Type = type });
            patch.Seed.Returns(new SeedDto
            {
                ItemID = 1, ProductID = 2, MinimumProductCount = 1, MaximumProductCount = 4, RequiredLevel = 1,
                PlantingExperience = 0, HarvestExperience = 100, ProductExperience = RepresentativeProductExperience,
                VarpBitIndex = 0, MaxCycles = 4, CycleTicks = 100, Type = type
            });
            patch.ProductCount.Returns(_ => remaining);
            patch.When(p => p.Harvest()).Do(_ => remaining--);
            patch.HasCondition(Arg.Any<PatchCondition>()).Returns(call =>
                call.Arg<PatchCondition>() is PatchCondition.Cleared or PatchCondition.Mature or PatchCondition.Checked);

            var character = Substitute.For<ICharacter>();
            character.Farming.GetFarmingPatch(7).Returns(patch);
            character.ServiceProvider.GetService(typeof(IItemBuilder)).Returns(Substitute.For<IItemBuilder>());
            character.ServiceProvider.GetService(typeof(IGroundItemBuilder)).Returns(Substitute.For<IGroundItemBuilder>());
            character.Inventory.AddItem(Arg.Any<IItem>()).Returns(true);
            var queued = new List<RsTickTask>();
            character.When(c => c.QueueTask(Arg.Any<ITaskItem>())).Do(call =>
            {
                if (call.Arg<ITaskItem>() is RsTickTask tick)
                {
                    queued.Add(tick);
                }
            });
            EventHappened handle = _ => true;
            var registered = new List<EventHappened<CreatureInterruptedEvent>>();
            character.RegisterEventHandler(Arg.Do<EventHappened<CreatureInterruptedEvent>>(registered.Add)).Returns(handle);
            var obj = Substitute.For<IGameObject>();
            obj.Id.Returns(7);
            return new Scenario(character, patch, obj, queued, handle, registered);
        }

        private static void Click(Scenario s) =>
            new FarmingSkillService(Substitute.For<IItemBuilder>())
                .HandlePatchClickPerform(s.Character, s.Obj, GameObjectClickType.Option1Click);

        [TestMethod]
        public void HandlePatchClickPerform_CheckedBush_AwardsProductExperienceForEachPickedBerryAndUnregistersOnCompletion()
        {
            var s = CreateCheckedPlant(PatchType.Bush, productCount: 2);

            Click(s);

            Assert.HasCount(1, s.Queued);
            for (var i = 0; i < 4; i++)
            {
                s.Queued[0].Tick();
            }

            s.Patch.Received(2).Harvest();
            s.Character.Statistics.Received(2).AddExperience(StatisticsConstants.Farming, RepresentativeProductExperience);
            s.Character.Statistics.DidNotReceive().AddExperience(StatisticsConstants.Farming, 100);
            Assert.IsTrue(s.Queued[0].IsCancelled);
            s.Character.Received(1).UnregisterEventHandler<CreatureInterruptedEvent>(s.Handle);
        }

        [TestMethod]
        public void HandlePatchClickPerform_CheckedBush_InterruptionCancelsPickingAndUnregistersOnce()
        {
            var s = CreateCheckedPlant(PatchType.Bush, productCount: 3);

            Click(s);
            s.Queued[0].Tick();
            s.Queued[0].Tick();
            s.Patch.Received(1).Harvest();

            s.Registered.Single()(new CreatureInterruptedEvent(s.Character, this));
            s.Queued[0].Tick();
            s.Queued[0].Tick();

            Assert.IsTrue(s.Queued[0].IsCancelled);
            s.Patch.Received(1).Harvest();
            s.Character.Received(1).UnregisterEventHandler<CreatureInterruptedEvent>(s.Handle);
        }

        private static void AllowFelling(Scenario s, int freeSlots)
        {
            var hatchet = new HatchetDto { ItemId = 1351, RequiredLevel = 1, Type = HatchetType.Bronze, ChopAnimationId = 879, CanoeAnimationId = 1, BaseHarvestChance = 0.1 };
            var woodcutting = Substitute.For<IWoodcuttingService>();
            woodcutting.FindAllHatchets().Returns(Task.FromResult((IReadOnlyList<HatchetDto>)new List<HatchetDto> { hatchet }));
            s.Character.ServiceProvider.GetService(typeof(IWoodcuttingService)).Returns(woodcutting);
            s.Character.Statistics.GetSkillLevel(Arg.Any<int>()).Returns(99);
            s.Character.Equipment.Items.GetById(hatchet.ItemId).Returns(Substitute.For<IItem>());
            s.Character.Inventory.Items.FreeSlots.Returns(freeSlots);
            s.Patch.FellTree().Returns(true);
            s.Character.QueueTask(Arg.Any<Func<CancellationToken, Task>>()).Returns(call =>
            {
                call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None).GetAwaiter().GetResult();
                return Substitute.For<IRsTaskHandle>();
            });
        }

        [TestMethod]
        public void HandlePatchClickPerform_CheckedTreeWithFullInventory_DoesNotFellTheTree()
        {
            var s = CreateCheckedPlant(PatchType.Tree, productCount: 1);
            AllowFelling(s, freeSlots: 0);

            Click(s);

            s.Patch.DidNotReceive().FellTree();
            s.Character.Received(1).SendChatMessage(GameStrings.InventoryFull, Arg.Any<ChatMessageType>(), null);
            Assert.IsEmpty(s.Queued);
        }

        [TestMethod]
        public void HandlePatchClickPerform_CheckedTreeFilledDuringSwing_DoesNotFellTheTree()
        {
            var s = CreateCheckedPlant(PatchType.Tree, productCount: 1);
            AllowFelling(s, freeSlots: 1);

            Click(s);
            s.Character.Inventory.Items.FreeSlots.Returns(0);
            for (var i = 0; i < 4; i++)
            {
                s.Queued.Single().Tick();
            }

            s.Patch.DidNotReceive().FellTree();
            s.Character.Received(1).SendChatMessage(GameStrings.InventoryFull, Arg.Any<ChatMessageType>(), null);
            s.Character.Received(1).UnregisterEventHandler<CreatureInterruptedEvent>(s.Handle);
        }

        [TestMethod]
        public void HandlePatchClickPerform_CheckedTreeWithSpace_FellsTheTreeOnce()
        {
            var s = CreateCheckedPlant(PatchType.Tree, productCount: 1);
            AllowFelling(s, freeSlots: 3);

            Click(s);
            for (var i = 0; i < 6; i++)
            {
                s.Queued.Single().Tick();
            }

            s.Patch.Received(1).FellTree();
            s.Character.Received(1).UnregisterEventHandler<CreatureInterruptedEvent>(s.Handle);
        }
    }
}
