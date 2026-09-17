using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Builders.Graphic;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class CreatureCombatTests
    {
        private ICharacter _mockOwner = null!;
        private ICreature _mockAttacker = null!;
        private ICreatureCombat _mockAttackerCombat = null!;
        private IViewport _mockViewport = null!;
        private TestableCharacterCombat _characterCombat = null!;
        private List<ICreature> _visibleCreatures = null!;
        private EntityStore _entityStore = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockOwner = EntityTestFactory.Create<ICharacter>();
            _mockAttacker = EntityTestFactory.Create<ICreature>();
            _mockAttackerCombat = Substitute.For<ICreatureCombat>();
            _mockViewport = Substitute.For<IViewport>();
            _visibleCreatures = new List<ICreature>();
            _entityStore = new EntityStore();

            _mockAttacker.Combat.Returns(_mockAttackerCombat);
            _mockOwner.Viewport.Returns(_mockViewport);
            _mockOwner.ServiceProvider.Returns(new ServiceCollection()
                .AddSingleton<IEntityStore>(_entityStore)
                .AddSingleton<IEntityService>(new EntityService(_entityStore))
                .BuildServiceProvider());
            _mockViewport.VisibleCreatures.Returns(_visibleCreatures);

            _characterCombat = CreateCombat();
        }

        [TestMethod]
        public void CanBeAttackedBy_OwnerIsDead_ReturnsFalse()
        {
            // Arrange
            _characterCombat.SetDead(true);

            // Act
            var result = _characterCombat.CanBeAttackedBy(_mockAttacker);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanBeAttackedBy_AttackerIsDead_ReturnsFalse()
        {
            // Arrange
            _mockAttackerCombat.IsDead.Returns(true);

            // Act
            var result = _characterCombat.CanBeAttackedBy(_mockAttacker);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanBeAttackedBy_AttackerNotInViewport_ReturnsFalse()
        {
            // Arrange

            // Act
            var result = _characterCombat.CanBeAttackedBy(_mockAttacker);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanBeAttackedBy_ScriptReturnsFalse_ReturnsFalse()
        {
            // Arrange
            _visibleCreatures.Add(_mockAttacker);
            var mockScript = Substitute.For<ICharacterScript>();
            mockScript.CanBeAttackedBy(_mockAttacker).Returns(false);
            _mockOwner.GetScripts().Returns(new[] { mockScript });

            // Act
            var result = _characterCombat.CanBeAttackedBy(_mockAttacker);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanBeAttackedBy_AllConditionsMet_ReturnsTrue()
        {
            // Arrange
            _visibleCreatures.Add(_mockAttacker);
            _mockOwner.GetScripts().Returns(new List<ICharacterScript>());

            // Act
            var result = _characterCombat.CanBeAttackedBy(_mockAttacker);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInCombat_HasTarget_ReturnsTrue()
        {
            // Arrange
            var mockTarget = EntityTestFactory.Create<ICharacter>();
            _characterCombat.SetTargetForTest(mockTarget);

            // Act
            var result = _characterCombat.IsInCombat();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInCombat_HasRecentAttackers_ReturnsTrue()
        {
            // Arrange
            _characterCombat.SetTargetForTest(null);
            _characterCombat.AddAttackerPublic(_mockAttacker);

            // Act
            var result = _characterCombat.IsInCombat();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInCombat_NoTargetAndNoRecentAttackers_ReturnsFalse()
        {
            // Arrange
            _characterCombat.SetTargetForTest(null);

            // Act
            var result = _characterCombat.IsInCombat();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RecentCharacterAttacker_AgesAndExpiresUsingCharacterTimeout()
        {
            var attacker = EntityTestFactory.Create<ICharacter>();
            _characterCombat.AddAttackerPublic(attacker);

            var attackerInfo = _characterCombat.RecentAttackers.Single();
            _characterCombat.Tick();
            Assert.AreEqual(1, attackerInfo.LastAttackTick);
            Assert.AreEqual(1, _characterCombat.RecentAttackersCount);

            _characterCombat.Tick();
            Assert.AreEqual(2, attackerInfo.LastAttackTick);
            Assert.AreEqual(1, _characterCombat.RecentAttackersCount);

            _characterCombat.Tick();
            Assert.AreEqual(0, _characterCombat.RecentAttackersCount);
        }

        [TestMethod]
        public void RecentNpcAttacker_AgesAndExpiresUsingNpcTimeout()
        {
            var attacker = EntityTestFactory.Create<INpc>();
            _characterCombat.AddAttackerPublic(attacker);

            var attackerInfo = _characterCombat.RecentAttackers.Single();
            _characterCombat.Tick();
            Assert.AreEqual(1, attackerInfo.LastAttackTick);
            Assert.AreEqual(1, _characterCombat.RecentAttackersCount);

            _characterCombat.Tick();
            Assert.AreEqual(2, attackerInfo.LastAttackTick);
            Assert.AreEqual(1, _characterCombat.RecentAttackersCount);

            _characterCombat.Tick();
            Assert.AreEqual(0, _characterCombat.RecentAttackersCount);
        }

        [TestMethod]
        public void AddAttacker_ResetsRecentRelationshipAge()
        {
            var attacker = EntityTestFactory.Create<ICharacter>();
            _characterCombat.AddAttackerPublic(attacker);
            _characterCombat.Tick();
            Assert.AreEqual(1, _characterCombat.RecentAttackers.Single().LastAttackTick);

            _characterCombat.AddAttackerPublic(attacker);

            Assert.AreEqual(0, _characterCombat.RecentAttackers.Single().LastAttackTick);
            _characterCombat.Tick();
            Assert.AreEqual(1, _characterCombat.RecentAttackersCount);
        }

        [TestMethod]
        public async Task RecentAttackerExpiry_DoesNotRemoveHistoricalDamageContribution()
        {
            var store = CreateCharacterStore();
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(store)
                .BuildServiceProvider();


            var attacker = EntityTestFactory.Create<ICharacter>();
            Assert.IsTrue(await store.AddAsync(attacker));
            _entityStore.Add(attacker);
            _characterCombat.AddAttackerPublic(attacker);
            _characterCombat.AddDamageToAttackerPublic(attacker, 10);

            _characterCombat.Tick();
            _characterCombat.Tick();
            _characterCombat.Tick();

            Assert.IsFalse(_characterCombat.IsInCombat());
            Assert.AreSame(attacker, _characterCombat.GetKiller());
        }

        [TestMethod]
        public void RecentAttackerExpiry_DoesNotResolveHistoricalDamageDuringTick()
        {
            var entityService = Substitute.For<IEntityService>();
            using var provider = new ServiceCollection()
                .AddSingleton(entityService)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);
            _characterCombat = CreateCombat();

            var attacker = EntityTestFactory.Create<ICharacter>();
            var handle = new EntityHandle(1, 1);
            attacker.Handle.Returns(handle);
            _characterCombat.AddAttackerPublic(attacker);
            _characterCombat.AddDamageToAttackerPublic(attacker, 10);
            _characterCombat.Tick();
            _characterCombat.Tick();
            _characterCombat.Tick();

            entityService.DidNotReceive().TryResolve<ICreature>(handle, out Arg.Any<ICreature>());
        }

        [TestMethod]
        public async Task GetKiller_SkipsStaleHighestDamageAndDoesNotAttributeReplacementByIndex()
        {
            var store = CreateCharacterStore();
            var entityStore = _entityStore;
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(store)
                .AddSingleton<IEntityStore>(entityStore)
                .AddSingleton<IEntityService>(new EntityService(entityStore))
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var staleAttacker = EntityTestFactory.Create<ICharacter>();
            var currentAttacker = EntityTestFactory.Create<ICharacter>();
            var replacement = EntityTestFactory.Create<ICharacter>();
            staleAttacker.MasterId.Returns(10u);
            currentAttacker.MasterId.Returns(11u);
            replacement.MasterId.Returns(12u);
            Assert.IsTrue(await store.AddAsync(staleAttacker));
            Assert.IsTrue(await store.AddAsync(currentAttacker));
            entityStore.Add(staleAttacker);
            entityStore.Add(currentAttacker);

            _characterCombat.AddAttackerPublic(staleAttacker);
            _characterCombat.AddDamageToAttackerPublic(staleAttacker, 100);
            _characterCombat.AddAttackerPublic(currentAttacker);
            _characterCombat.AddDamageToAttackerPublic(currentAttacker, 10);

            Assert.IsTrue(store.Remove(staleAttacker));
            Assert.IsTrue(entityStore.Remove(staleAttacker));
            Assert.IsTrue(await store.AddAsync(replacement));
            entityStore.Add(replacement);
            Assert.AreEqual(staleAttacker.Index, replacement.Index);

            Assert.AreSame(currentAttacker, _characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task GetKiller_PreservesFamiliarAttributionForCurrentNpcHandle()
        {
            var store = new NpcStore();
            var entityStore = _entityStore;
            using var provider = new ServiceCollection()
                .AddSingleton<INpcStore>(store)
                .AddSingleton<IEntityStore>(entityStore)
                .AddSingleton<IEntityService>(new EntityService(entityStore))
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var familiar = EntityTestFactory.Create<INpc>();
            var familiarScript = Substitute.For<IFamiliarScript>();
            var summoner = EntityTestFactory.Create<ICharacter>();
            familiarScript.Summoner.Returns(summoner);
            familiar.TryGetScript<IFamiliarScript>(out Arg.Any<IFamiliarScript>())
                .Returns(callInfo =>
                {
                    callInfo[0] = familiarScript;
                    return true;
                });
            Assert.IsTrue(await store.AddAsync(familiar));
            entityStore.Add(familiar);

            _characterCombat.AddAttackerPublic(familiar);
            _characterCombat.AddDamageToAttackerPublic(familiar, 25);

            Assert.AreSame(summoner, _characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task GetKiller_ResolvesCurrentNpcDamageContribution()
        {
            var store = new NpcStore();
            var entityStore = _entityStore;
            using var provider = new ServiceCollection()
                .AddSingleton<INpcStore>(store)
                .AddSingleton<IEntityStore>(entityStore)
                .AddSingleton<IEntityService>(new EntityService(entityStore))
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var npc = EntityTestFactory.Create<INpc>();
            Assert.IsTrue(await store.AddAsync(npc));
            entityStore.Add(npc);

            _characterCombat.AddAttackerPublic(npc);
            _characterCombat.AddDamageToAttackerPublic(npc, 25);

            Assert.AreSame(npc, _characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task GetKiller_SkipsStaleHighestNpcDamageAndUsesNextValidContribution()
        {
            var store = new NpcStore();
            var entityStore = _entityStore;
            using var provider = new ServiceCollection()
                .AddSingleton<INpcStore>(store)
                .AddSingleton<IEntityStore>(entityStore)
                .AddSingleton<IEntityService>(new EntityService(entityStore))
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var staleNpc = EntityTestFactory.Create<INpc>();
            var currentNpc = EntityTestFactory.Create<INpc>();
            var replacementNpc = EntityTestFactory.Create<INpc>();
            Assert.IsTrue(await store.AddAsync(staleNpc));
            Assert.IsTrue(await store.AddAsync(currentNpc));
            entityStore.Add(staleNpc);
            entityStore.Add(currentNpc);

            _characterCombat.AddAttackerPublic(staleNpc);
            _characterCombat.AddDamageToAttackerPublic(staleNpc, 100);
            _characterCombat.AddAttackerPublic(currentNpc);
            _characterCombat.AddDamageToAttackerPublic(currentNpc, 10);

            Assert.IsTrue(store.Remove(staleNpc));
            Assert.IsTrue(entityStore.Remove(staleNpc));
            Assert.IsTrue(await store.AddAsync(replacementNpc));
            entityStore.Add(replacementNpc);
            Assert.AreEqual(staleNpc.Index, replacementNpc.Index);

            Assert.AreSame(currentNpc, _characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task GetKiller_SkipsStaleFamiliarHandleAndDoesNotAttributeReplacementNpc()
        {
            var store = new NpcStore();
            var entityStore = _entityStore;
            using var provider = new ServiceCollection()
                .AddSingleton<INpcStore>(store)
                .AddSingleton<IEntityStore>(entityStore)
                .AddSingleton<IEntityService>(new EntityService(entityStore))
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var familiar = EntityTestFactory.Create<INpc>();
            var replacement = EntityTestFactory.Create<INpc>();
            Assert.IsTrue(await store.AddAsync(familiar));
            entityStore.Add(familiar);

            _characterCombat.AddAttackerPublic(familiar);
            _characterCombat.AddDamageToAttackerPublic(familiar, 25);

            Assert.IsTrue(store.Remove(familiar));
            Assert.IsTrue(entityStore.Remove(familiar));
            Assert.IsTrue(await store.AddAsync(replacement));
            entityStore.Add(replacement);
            Assert.AreEqual(familiar.Index, replacement.Index);

            Assert.IsNull(_characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task DamageHistory_ExpiresAfterFiveMinutesWithoutResolvingEveryTick()
        {
            var store = CreateCharacterStore();
            var entityStore = _entityStore;
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(store)
                .AddSingleton<IEntityStore>(entityStore)
                .AddSingleton<IEntityService>(new EntityService(entityStore))
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);
            var attacker = EntityTestFactory.Create<ICharacter>();
            attacker.MasterId.Returns(13u);
            Assert.IsTrue(await store.AddAsync(attacker));
            entityStore.Add(attacker);

            _characterCombat.AddAttackerPublic(attacker);
            _characterCombat.AddDamageToAttackerPublic(attacker, 10);

            for (var tick = 0; tick < 500; tick++)
            {
                _characterCombat.Tick();
            }

            Assert.IsNull(_characterCombat.GetKiller());
        }

        [TestMethod]
        public void OnDestroy_RemovesOwnedCombatRelationships()
        {
            _characterCombat.SetTargetForTest(_mockAttacker);
            _characterCombat.SetLastAttackedForTest(_mockAttacker);
            _characterCombat.AddAttackerPublic(_mockAttacker);

            _characterCombat.OnDestroy();

            Assert.IsNull(_characterCombat.Target);
            Assert.IsNull(_characterCombat.LastAttacked);
            Assert.AreEqual(0, _characterCombat.RecentAttackersCount);
            Assert.IsNull(_characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task CanSetTarget_WhenCharacterTargetWasRemoved_ReturnsFalse()
        {
            var store = CreateCharacterStore();
            var entityStore = _entityStore;
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(store)
                .AddSingleton<IEntityStore>(entityStore)
                .AddSingleton<IEntityService>(new EntityService(entityStore))
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);
            var target = EntityTestFactory.Create<ICharacter>();
            target.MasterId.Returns(14u);
            Assert.IsTrue(await store.AddAsync(target));
            entityStore.Add(target);
            _characterCombat.SetTargetForTest(target);

            Assert.IsTrue(store.Remove(target));
            Assert.IsTrue(entityStore.Remove(target));

            Assert.IsFalse(_characterCombat.CanSetTarget(target.Handle));
        }

        [TestMethod]
        public async Task CanSetTarget_WhenNpcTargetWasRemoved_ReturnsFalse()
        {
            var store = new NpcStore();
            var entityStore = _entityStore;
            using var provider = new ServiceCollection()
                .AddSingleton<INpcStore>(store)
                .AddSingleton<IEntityStore>(entityStore)
                .AddSingleton<IEntityService>(new EntityService(entityStore))
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);
            var target = EntityTestFactory.Create<INpc>();
            Assert.IsTrue(await store.AddAsync(target));
            entityStore.Add(target);
            _characterCombat.SetTargetForTest(target);

            Assert.IsTrue(store.Remove(target));
            Assert.IsTrue(entityStore.Remove(target));

            Assert.IsFalse(_characterCombat.CanSetTarget(target.Handle));
        }

        [TestMethod]
        public async Task CanSetTarget_WhenCharacterSlotIsReused_DoesNotAcceptOriginalTarget()
        {
            var store = CreateCharacterStore();
            var entityStore = _entityStore;
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(store)
                .AddSingleton<IEntityStore>(entityStore)
                .AddSingleton<IEntityService>(new EntityService(entityStore))
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);
            var target = EntityTestFactory.Create<ICharacter>();
            var replacement = EntityTestFactory.Create<ICharacter>();
            target.MasterId.Returns(15u);
            replacement.MasterId.Returns(16u);
            Assert.IsTrue(await store.AddAsync(target));
            entityStore.Add(target);
            _characterCombat.SetTargetForTest(target);

            Assert.IsTrue(store.Remove(target));
            Assert.IsTrue(entityStore.Remove(target));
            Assert.IsTrue(await store.AddAsync(replacement));
            entityStore.Add(replacement);
            Assert.AreEqual(target.Index, replacement.Index);

            Assert.IsFalse(_characterCombat.CanSetTarget(target.Handle));
            Assert.IsNull(_characterCombat.Target);
        }

        [TestMethod]
        public async Task CanSetTarget_WhenNpcSlotIsReused_DoesNotAcceptOriginalTarget()
        {
            var store = new NpcStore();
            var entityStore = _entityStore;
            using var provider = new ServiceCollection()
                .AddSingleton<INpcStore>(store)
                .AddSingleton<IEntityStore>(entityStore)
                .AddSingleton<IEntityService>(new EntityService(entityStore))
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);
            var target = EntityTestFactory.Create<INpc>();
            var replacement = EntityTestFactory.Create<INpc>();
            Assert.IsTrue(await store.AddAsync(target));
            entityStore.Add(target);
            _characterCombat.SetTargetForTest(target);

            Assert.IsTrue(store.Remove(target));
            Assert.IsTrue(entityStore.Remove(target));
            Assert.IsTrue(await store.AddAsync(replacement));
            entityStore.Add(replacement);
            Assert.AreEqual(target.Index, replacement.Index);

            Assert.IsFalse(_characterCombat.CanSetTarget(target.Handle));
            Assert.IsNull(_characterCombat.Target);
        }

        [TestMethod]
        public void Tick_ClearsLastAttackedWhenPeerNoLongerListsOwner()
        {
            _mockAttackerCombat.RecentAttackers.Returns(Array.Empty<ICreatureAttackerInfo>());
            _characterCombat.SetLastAttackedForTest(_mockAttacker);

            _characterCombat.Tick();

            Assert.IsNull(_characterCombat.LastAttacked);
        }

        [TestMethod]
        public void LastAttacked_FadesAfterReciprocalRecentAttackerExpires()
        {
            var reciprocalAttackers = new List<ICreatureAttackerInfo>
            {
                new CreatureAttackerInfo(_mockOwner, 0)
            };
            _mockAttackerCombat.RecentAttackers.Returns(_ => reciprocalAttackers);
            _characterCombat.SetLastAttackedForTest(_mockAttacker);

            _characterCombat.Tick();
            Assert.AreSame(_mockAttacker, _characterCombat.LastAttacked);

            reciprocalAttackers.Clear();
            _characterCombat.Tick();

            Assert.IsNull(_characterCombat.LastAttacked);
        }

        [TestMethod]
        public async Task GetKiller_DoesNotRequireAttackerToRemainVisible()
        {
            var store = CreateCharacterStore();
            var entityStore = _entityStore;
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(store)
                .AddSingleton<IEntityStore>(entityStore)
                .AddSingleton<IEntityService>(new EntityService(entityStore))
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var attacker = EntityTestFactory.Create<ICharacter>();
            Assert.IsTrue(await store.AddAsync(attacker));
            entityStore.Add(attacker);
            _characterCombat.AddAttackerPublic(attacker);
            _characterCombat.AddDamageToAttackerPublic(attacker, 10);
            _visibleCreatures.Clear();

            Assert.AreSame(attacker, _characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task DamageHistory_StoresTypeAgnosticHandlesInsteadOfCreatureReferences()
        {
            var characterStore = CreateCharacterStore();
            var npcStore = new NpcStore();
            var entityStore = _entityStore;
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(characterStore)
                .AddSingleton<INpcStore>(npcStore)
                .AddSingleton<IEntityStore>(entityStore)
                .AddSingleton<IEntityService>(new EntityService(entityStore))
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var character = EntityTestFactory.Create<ICharacter>();
            var npc = EntityTestFactory.Create<INpc>();
            Assert.IsTrue(await characterStore.AddAsync(character));
            Assert.IsTrue(await npcStore.AddAsync(npc));
            entityStore.Add(character);
            entityStore.Add(npc);

            _characterCombat.AddAttackerPublic(character);
            _characterCombat.AddDamageToAttackerPublic(character, 10);
            _characterCombat.AddAttackerPublic(npc);
            _characterCombat.AddDamageToAttackerPublic(npc, 10);

            foreach (var field in typeof(Hagalaz.Services.GameWorld.Model.Creatures.CreatureCombat)
                         .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                         .Where(field => field.Name.Contains("DamageContributions", StringComparison.Ordinal)))
            {
                var entries = (System.Collections.IEnumerable)field.GetValue(_characterCombat)!;
                foreach (var entry in entries)
                {
                    var attackerField = entry.GetType().GetField("<Attacker>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    Assert.IsNotNull(attackerField);
                    Assert.AreEqual(typeof(EntityHandle), attackerField!.FieldType);
                    Assert.IsFalse(typeof(ICreature).IsAssignableFrom(attackerField.FieldType));
                }
            }
        }

        [TestMethod]
        public void PerformAttack_WhenTargetHandleIsStale_DoesNotInvokeIncomingAttack()
        {
            var target = EntityTestFactory.Create<ICharacter>();
            var targetCombat = Substitute.For<ICreatureCombat>();
            target.Combat.Returns(targetCombat);
            _entityStore.Add(target);
            var targetHandle = target.Handle;
            Assert.IsTrue(_entityStore.Remove(target));

            _mockOwner.QueueTask(Arg.Any<ITaskItem<AttackResult>>())
                .Returns(Substitute.For<IRsTaskHandle<AttackResult>>());

            _characterCombat.PerformAttack(new AttackParams
            {
                Target = targetHandle,
                DamageType = DamageType.StandardMelee,
                Damage = 1
            });

            targetCombat.DidNotReceiveWithAnyArgs().IncomingAttack(default!, default, default, default);
        }

        [TestMethod]
        public void PerformAttack_WhenTargetIsRemovedBeforeDelayedExecution_DoesNotAttackReplacement()
        {
            var target = EntityTestFactory.Create<ICharacter>();
            var targetCombat = Substitute.For<ICreatureCombat>();
            target.Combat.Returns(targetCombat);
            target.Location.Returns(Location.Create(3200, 3200));
            targetCombat.IncomingAttack(Arg.Any<ICreature>(), Arg.Any<DamageType>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(0);

            var replacement = EntityTestFactory.Create<ICharacter>();
            var replacementCombat = Substitute.For<ICreatureCombat>();
            replacement.Combat.Returns(replacementCombat);

            _entityStore.Add(target);
            var targetHandle = target.Handle;
            ITaskItem<AttackResult>? delayedTask = null;
            target.QueueTask(Arg.Do<ITaskItem<AttackResult>>(task => delayedTask = task))
                .Returns(callInfo => new RsTaskHandle<AttackResult>(callInfo.Arg<ITaskItem<AttackResult>>()!));

            _characterCombat.PerformAttack(new AttackParams
            {
                Target = targetHandle,
                DamageType = DamageType.StandardMelee,
                Damage = 0
            });

            Assert.IsNotNull(delayedTask);
            Assert.IsTrue(_entityStore.Remove(target));
            _entityStore.Add(replacement);
            Assert.AreEqual(target.Index, replacement.Index);

            delayedTask!.Tick();

            var soak = 0;
            targetCombat.DidNotReceiveWithAnyArgs().Attack(default!, default, default, ref soak);
            replacementCombat.DidNotReceiveWithAnyArgs().Attack(default!, default, default, ref soak);
        }

        private static CharacterStore CreateCharacterStore() => new(Options.Create(new GameServerOptions
        {
            ClientRevision = 1,
            ClientRevisionPatch = 0,
            AuthenticationToken = "test"
        }));

        private TestableCharacterCombat CreateCombat() => new(
            _mockOwner,
            _mockOwner.ServiceProvider.GetRequiredService<IEntityService>(),
            Substitute.For<IAnimationBuilder>(),
            Substitute.For<IGraphicBuilder>(),
            Substitute.For<IProjectileBuilder>(),
            Substitute.For<IMapRegionService>(),
            Substitute.For<IGroundItemBuilder>(),
            Substitute.For<IHitSplatBuilder>(),
            Substitute.For<IProjectilePathFinder>(),
            Substitute.For<ISmartPathFinder>(),
            Options.Create(new CombatOptions
            {
                CharacterAttackTickDelay = 1,
                NpcAttackTickDelay = 1
            }));
    }

    public class TestableCharacterCombat : CharacterCombat
    {
        public TestableCharacterCombat(
            ICharacter owner,
            IEntityService entityService,
            IAnimationBuilder animationBuilder,
            IGraphicBuilder graphicBuilder,
            IProjectileBuilder projectileBuilder,
            IMapRegionService mapRegionService,
            IGroundItemBuilder groundItemBuilder,
            IHitSplatBuilder hitSplatBuilder,
            IProjectilePathFinder projectilePathFinder,
            ISmartPathFinder smartPathFinder,
            IOptions<CombatOptions> combatOptions)
            : base(owner, entityService, animationBuilder, graphicBuilder, projectileBuilder, mapRegionService, groundItemBuilder,
                hitSplatBuilder, projectilePathFinder, smartPathFinder, combatOptions)
        {
        }

        public void SetDead(bool isDead)
        {
            IsDead = isDead;
        }

        public void SetTargetForTest(ICreature? target)
        {
            if (target is not null)
            {
                Owner.ServiceProvider.GetService<IEntityStore>()?.Add(target);
            }

            SetTargetHandle(target?.Handle ?? default);
        }

        public void AddAttackerPublic(ICreature attacker)
        {
            Owner.ServiceProvider.GetService<IEntityStore>()?.Add(attacker);
            AddAttacker(attacker);
        }

        public void AddDamageToAttackerPublic(ICreature attacker, int damage)
        {
            AddDamageToAttacker(attacker, damage);
        }

        public void SetLastAttackedForTest(ICreature? creature)
        {
            LastAttacked = creature;
        }
    }
}
