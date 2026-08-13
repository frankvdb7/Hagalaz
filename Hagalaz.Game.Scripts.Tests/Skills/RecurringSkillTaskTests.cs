using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Fishing;
using Hagalaz.Game.Scripts.Skills.Mining;
using Hagalaz.Game.Scripts.Skills.Woodcutting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills
{
    [TestClass]
    public sealed class RecurringSkillTaskTests
    {
        [TestMethod]
        public void MiningTask_Tick_CompletesRewardBeforeReturning()
        {
            // Arrange
            var performer = Substitute.For<ICharacter>();
            var rocks = Substitute.For<IGameObject>();
            var tickReturned = false;
            var callbackCompletedBeforeTickReturned = false;
            var callbackCalls = 0;
            bool FinishReward()
            {
                callbackCalls++;
                callbackCompletedBeforeTickReturned = !tickReturned;
                return false;
            }

            var task = new MiningTask(
                performer,
                FinishReward,
                chance: 1.0,
                new PickaxeDto
                {
                    Type = PickaxeType.Bronze,
                    ItemId = 1,
                    AnimationId = 1,
                    RequiredLevel = 1,
                    BaseHarvestChance = 0.1,
                },
                rocks);

            // Act
            task.Tick();
            tickReturned = true;

            // Assert
            Assert.AreEqual(1, callbackCalls);
            Assert.IsTrue(callbackCompletedBeforeTickReturned);
        }

        [TestMethod]
        public void FishingTask_TwoTicks_DoNotOverlapRewardCallbacks()
        {
            // Arrange
            var performer = Substitute.For<ICharacter>();
            var fishingSpot = Substitute.For<INpc>();
            var viewport = Substitute.For<IViewport>();
            var movement = Substitute.For<IMovement>();
            performer.Viewport.Returns(viewport);
            viewport.VisibleCreatures.Returns(new List<ICreature> { fishingSpot });
            fishingSpot.Movement.Returns(movement);

            var callbackCalls = 0;
            var callbackActive = false;
            var callbacksOverlapped = false;
            bool FinishReward()
            {
                if (callbackActive)
                {
                    callbacksOverlapped = true;
                }

                callbackActive = true;
                callbackCalls++;
                callbackActive = false;
                return false;
            }

            var task = new FishingTask(performer, FinishReward, chance: 1.0, fishingSpot, animId: 1);

            // Act
            task.Tick();
            task.Tick();

            // Assert
            Assert.AreEqual(2, callbackCalls);
            Assert.IsFalse(callbacksOverlapped);
        }

        [TestMethod]
        public void WoodcuttingTask_Cancel_PreventsFurtherRewardCallbacks()
        {
            // Arrange
            var performer = Substitute.For<ICharacter>();
            var tree = Substitute.For<IGameObject>();
            var callbackCalls = 0;
            bool FinishReward()
            {
                callbackCalls++;
                return false;
            }

            var task = new WoodcuttingTask(
                performer,
                FinishReward,
                chance: 1.0,
                new HatchetDto
                {
                    Type = HatchetType.Bronze,
                    ItemId = 1,
                    ChopAnimationId = 1,
                    CanoeAnimationId = 1,
                    RequiredLevel = 1,
                    BaseHarvestChance = 0.1,
                },
                tree,
                ivyTree: false);

            // Act
            task.Tick();
            task.Cancel();
            task.Tick();

            // Assert
            Assert.AreEqual(1, callbackCalls);
            Assert.IsTrue(task.IsCancelled);
            Assert.IsTrue(task.IsCompleted);
        }
    }
}
