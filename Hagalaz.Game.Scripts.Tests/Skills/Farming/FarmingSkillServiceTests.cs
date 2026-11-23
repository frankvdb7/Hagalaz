using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
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

        [TestMethod]
        public void HandlePatchClickPerform_CheckedBush_AwardsProductExperienceForEachPickedBerry()
        {
            var remaining = 2;
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
            var obj = Substitute.For<IGameObject>();
            obj.Id.Returns(7);

            new FarmingSkillService(Substitute.For<IItemBuilder>())
                .HandlePatchClickPerform(character, obj, GameObjectClickType.Option1Click);

            Assert.HasCount(1, queued);
            for (var i = 0; i < 4; i++)
            {
                queued[0].Tick();
            }

            patch.Received(2).Harvest();
            character.Statistics.Received(2).AddExperience(StatisticsConstants.Farming, RepresentativeProductExperience);
            character.Statistics.DidNotReceive().AddExperience(StatisticsConstants.Farming, 100);
            Assert.IsTrue(queued[0].IsCancelled);
        }
    }
}
