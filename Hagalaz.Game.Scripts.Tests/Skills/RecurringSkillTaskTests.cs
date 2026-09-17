using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
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
            ConfigureTarget(performer, rocks, new EntityHandle<IGameObject>(1, 1));
            var tickReturned = false;
            var callbackCompletedBeforeTickReturned = false;
            var callbackCalls = 0;
            bool FinishReward(IGameObject target)
            {
                Assert.AreSame(rocks, target);
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
                rocks.Handle);

            // Act
            task.Tick();
            tickReturned = true;

            // Assert
            Assert.AreEqual(1, callbackCalls);
            Assert.IsTrue(callbackCompletedBeforeTickReturned);
        }

        [TestMethod]
        public void MiningTask_StaleTarget_CancelsBeforeRewardOrAnimation()
        {
            var performer = Substitute.For<ICharacter>();
            var entityService = Substitute.For<IEntityService>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
            performer.ServiceProvider.Returns(serviceProvider);
            var handle = new EntityHandle<IGameObject>(2, 1);
            entityService.TryResolve<IGameObject>(handle, out Arg.Any<IGameObject>()).Returns(false);
            var callbackCalls = 0;
            var task = new MiningTask(performer, _ =>
            {
                callbackCalls++;
                return true;
            }, 1.0, new PickaxeDto
            {
                Type = PickaxeType.Bronze,
                ItemId = 1,
                RequiredLevel = 1,
                BaseHarvestChance = 0.1,
                AnimationId = 1,
            }, handle);

            task.Tick();

            Assert.IsTrue(task.IsCancelled);
            Assert.AreEqual(0, callbackCalls);
            performer.DidNotReceive().QueueAnimation(Arg.Any<IAnimation>());
        }

        [TestMethod]
        public void MiningTask_DisabledTarget_CancelsEvenWhenHarvestChanceDoesNotRoll()
        {
            var performer = Substitute.For<ICharacter>();
            var target = Substitute.For<IGameObject>();
            target.IsDisabled.Returns(true);
            var handle = new EntityHandle<IGameObject>(3, 1);
            ConfigureTarget(performer, target, handle);
            var callbackCalls = 0;
            var task = new MiningTask(performer, _ =>
            {
                callbackCalls++;
                return true;
            }, -1.0, new PickaxeDto
            {
                Type = PickaxeType.Bronze,
                ItemId = 1,
                RequiredLevel = 1,
                BaseHarvestChance = 0.1,
                AnimationId = 1,
            }, handle);

            task.Tick();

            Assert.IsTrue(task.IsCancelled);
            Assert.AreEqual(0, callbackCalls);
            performer.DidNotReceive().QueueAnimation(Arg.Any<IAnimation>());
        }

        [TestMethod]
        public void MiningTask_StaleHandleDoesNotUseReplacement()
        {
            var performer = Substitute.For<ICharacter>();
            var entityService = Substitute.For<IEntityService>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
            performer.ServiceProvider.Returns(serviceProvider);
            var staleHandle = new EntityHandle<IGameObject>(4, 1);
            var replacement = Substitute.For<IGameObject>();
            entityService.TryResolve<IGameObject>(staleHandle, out Arg.Any<IGameObject>()).Returns(false);
            var callbackCalls = 0;
            var task = new MiningTask(performer, target =>
            {
                callbackCalls++;
                Assert.AreSame(replacement, target);
                return true;
            }, 1.0, new PickaxeDto
            {
                Type = PickaxeType.Bronze,
                ItemId = 1,
                RequiredLevel = 1,
                BaseHarvestChance = 0.1,
                AnimationId = 1,
            }, staleHandle);

            task.Tick();

            Assert.IsTrue(task.IsCancelled);
            Assert.AreEqual(0, callbackCalls);
            performer.DidNotReceive().QueueAnimation(Arg.Any<IAnimation>());
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
            ConfigureTarget(performer, tree, new EntityHandle<IGameObject>(5, 1));
            var callbackCalls = 0;
            bool FinishReward(IGameObject target)
            {
                Assert.AreSame(tree, target);
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
                tree.Handle,
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

        [TestMethod]
        public void WoodcuttingTask_StaleTarget_CancelsBeforeRewardOrAnimation()
        {
            var performer = Substitute.For<ICharacter>();
            var entityService = Substitute.For<IEntityService>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
            performer.ServiceProvider.Returns(serviceProvider);
            var handle = new EntityHandle<IGameObject>(6, 1);
            entityService.TryResolve<IGameObject>(handle, out Arg.Any<IGameObject>()).Returns(false);
            var callbackCalls = 0;
            var task = new WoodcuttingTask(performer, _ =>
            {
                callbackCalls++;
                return true;
            }, 1.0, new HatchetDto
            {
                Type = HatchetType.Bronze,
                ItemId = 1,
                CanoeAnimationId = 1,
                RequiredLevel = 1,
                BaseHarvestChance = 0.1,
                ChopAnimationId = 1,
            }, handle, false);

            task.Tick();

            Assert.IsTrue(task.IsCancelled);
            Assert.AreEqual(0, callbackCalls);
            performer.DidNotReceive().QueueAnimation(Arg.Any<IAnimation>());
        }

        [TestMethod]
        public void WoodcuttingTask_DisabledTarget_CancelsEvenWhenHarvestChanceDoesNotRoll()
        {
            var performer = Substitute.For<ICharacter>();
            var target = Substitute.For<IGameObject>();
            target.IsDisabled.Returns(true);
            var handle = new EntityHandle<IGameObject>(7, 1);
            ConfigureTarget(performer, target, handle);
            var callbackCalls = 0;
            var task = new WoodcuttingTask(performer, _ =>
            {
                callbackCalls++;
                return true;
            }, -1.0, new HatchetDto
            {
                Type = HatchetType.Bronze,
                ItemId = 1,
                CanoeAnimationId = 1,
                RequiredLevel = 1,
                BaseHarvestChance = 0.1,
                ChopAnimationId = 1,
            }, handle, false);

            task.Tick();

            Assert.IsTrue(task.IsCancelled);
            Assert.AreEqual(0, callbackCalls);
            performer.DidNotReceive().QueueAnimation(Arg.Any<IAnimation>());
        }

        [TestMethod]
        public void WoodcuttingTask_StaleHandleDoesNotUseReplacement()
        {
            var performer = Substitute.For<ICharacter>();
            var entityService = Substitute.For<IEntityService>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
            performer.ServiceProvider.Returns(serviceProvider);
            var staleHandle = new EntityHandle<IGameObject>(8, 1);
            var replacement = Substitute.For<IGameObject>();
            entityService.TryResolve<IGameObject>(staleHandle, out Arg.Any<IGameObject>()).Returns(false);
            var callbackCalls = 0;
            var task = new WoodcuttingTask(performer, target =>
            {
                callbackCalls++;
                Assert.AreSame(replacement, target);
                return true;
            }, 1.0, new HatchetDto
            {
                Type = HatchetType.Bronze,
                ItemId = 1,
                CanoeAnimationId = 1,
                RequiredLevel = 1,
                BaseHarvestChance = 0.1,
                ChopAnimationId = 1,
            }, staleHandle, false);

            task.Tick();

            Assert.IsTrue(task.IsCancelled);
            Assert.AreEqual(0, callbackCalls);
            performer.DidNotReceive().QueueAnimation(Arg.Any<IAnimation>());
        }

        private static IEntityService ConfigureTarget(
            ICharacter performer,
            IGameObject target,
            EntityHandle<IGameObject> handle)
        {
            var entityService = Substitute.For<IEntityService>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
            performer.ServiceProvider.Returns(serviceProvider);
            target.Handle.Returns(handle);
            entityService.TryResolve<IGameObject>(handle, out Arg.Any<IGameObject>()).Returns(callInfo =>
            {
                callInfo[1] = target;
                return true;
            });
            return entityService;
        }
    }
}
