using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Scripts.Skills.Farming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;

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

        private static Scenario CreateCheckedBush(int productCount)
        {
            var remaining = productCount;
            var patch = Substitute.For<IFarmingPatch>();
            patch.PatchDefinition.Returns(new PatchDto { ObjectID = 7, Type = PatchType.Bush });
            patch.Seed.Returns(new SeedDto
            {
                ItemID = 1, ProductID = 2, MinimumProductCount = 1, MaximumProductCount = 4, RequiredLevel = 1,
                PlantingExperience = 0, HarvestExperience = 100, ProductExperience = RepresentativeProductExperience,
                VarpBitIndex = 0, MaxCycles = 4, CycleTicks = 100, Type = PatchType.Bush
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
            var s = CreateCheckedBush(productCount: 2);

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
            var s = CreateCheckedBush(productCount: 3);

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
    }
}
