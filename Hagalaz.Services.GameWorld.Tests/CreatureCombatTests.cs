using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Builders.Graphic;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Model.Creatures;
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

        [TestInitialize]
        public void TestInitialize()
        {
            _mockOwner = Substitute.For<ICharacter>();
            _mockAttacker = Substitute.For<ICreature>();
            _mockAttackerCombat = Substitute.For<ICreatureCombat>();
            _mockViewport = Substitute.For<IViewport>();
            _visibleCreatures = new List<ICreature>();

            _mockAttacker.Combat.Returns(_mockAttackerCombat);
            _mockOwner.Viewport.Returns(_mockViewport);
            _mockViewport.VisibleCreatures.Returns(_visibleCreatures);

            _characterCombat = new TestableCharacterCombat(
                _mockOwner,
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
            var mockTarget = Substitute.For<ICreature>();
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
            var attacker = Substitute.For<ICharacter>();
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
            var attacker = Substitute.For<INpc>();
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
            var attacker = Substitute.For<ICharacter>();
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
            _mockOwner.ServiceProvider.Returns(provider);

            var attacker = Substitute.For<ICharacter>();
            Assert.IsTrue(await store.AddAsync(attacker));
            _characterCombat.AddAttackerPublic(attacker);
            _characterCombat.AddDamageToAttackerPublic(attacker, 10);

            _characterCombat.Tick();
            _characterCombat.Tick();
            _characterCombat.Tick();

            Assert.IsFalse(_characterCombat.IsInCombat());
            Assert.AreSame(attacker, _characterCombat.GetKiller());
        }

        [TestMethod]
        public void RecentAttackerExpiry_DoesNotResolveStores()
        {
            var characterStore = Substitute.For<ICharacterStore>();
            using var provider = new ServiceCollection()
                .AddSingleton(characterStore)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var attacker = Substitute.For<ICharacter>();
            _characterCombat.AddAttackerPublic(attacker);
            characterStore.ClearReceivedCalls();

            _characterCombat.Tick();
            _characterCombat.Tick();
            _characterCombat.Tick();

            characterStore.DidNotReceive().Resolve(Arg.Any<CreatureHandle<ICharacter>>());
        }

        [TestMethod]
        public async Task GetKiller_SkipsStaleHighestDamageAndDoesNotAttributeReplacementByIndex()
        {
            var store = CreateCharacterStore();
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(store)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var staleAttacker = Substitute.For<ICharacter>();
            var currentAttacker = Substitute.For<ICharacter>();
            var replacement = Substitute.For<ICharacter>();
            staleAttacker.MasterId.Returns(10u);
            currentAttacker.MasterId.Returns(11u);
            replacement.MasterId.Returns(12u);
            Assert.IsTrue(await store.AddAsync(staleAttacker));
            Assert.IsTrue(await store.AddAsync(currentAttacker));

            _characterCombat.AddAttackerPublic(staleAttacker);
            _characterCombat.AddDamageToAttackerPublic(staleAttacker, 100);
            _characterCombat.AddAttackerPublic(currentAttacker);
            _characterCombat.AddDamageToAttackerPublic(currentAttacker, 10);

            Assert.IsTrue(store.Remove(staleAttacker));
            Assert.IsTrue(await store.AddAsync(replacement));
            Assert.AreEqual(staleAttacker.Index, replacement.Index);

            Assert.AreSame(currentAttacker, _characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task GetKiller_PreservesFamiliarAttributionForCurrentNpcHandle()
        {
            var store = new NpcStore();
            using var provider = new ServiceCollection()
                .AddSingleton<INpcStore>(store)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var familiar = Substitute.For<INpc>();
            var familiarScript = Substitute.For<IFamiliarScript>();
            var summoner = Substitute.For<ICharacter>();
            familiarScript.Summoner.Returns(summoner);
            familiar.TryGetScript<IFamiliarScript>(out Arg.Any<IFamiliarScript>())
                .Returns(callInfo =>
                {
                    callInfo[0] = familiarScript;
                    return true;
                });
            Assert.IsTrue(await store.AddAsync(familiar));

            _characterCombat.AddAttackerPublic(familiar);
            _characterCombat.AddDamageToAttackerPublic(familiar, 25);

            Assert.AreSame(summoner, _characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task GetKiller_ResolvesCurrentNpcDamageContribution()
        {
            var store = new NpcStore();
            using var provider = new ServiceCollection()
                .AddSingleton<INpcStore>(store)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var npc = Substitute.For<INpc>();
            Assert.IsTrue(await store.AddAsync(npc));

            _characterCombat.AddAttackerPublic(npc);
            _characterCombat.AddDamageToAttackerPublic(npc, 25);

            Assert.AreSame(npc, _characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task GetKiller_SkipsStaleHighestNpcDamageAndUsesNextValidContribution()
        {
            var store = new NpcStore();
            using var provider = new ServiceCollection()
                .AddSingleton<INpcStore>(store)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var staleNpc = Substitute.For<INpc>();
            var currentNpc = Substitute.For<INpc>();
            var replacementNpc = Substitute.For<INpc>();
            Assert.IsTrue(await store.AddAsync(staleNpc));
            Assert.IsTrue(await store.AddAsync(currentNpc));

            _characterCombat.AddAttackerPublic(staleNpc);
            _characterCombat.AddDamageToAttackerPublic(staleNpc, 100);
            _characterCombat.AddAttackerPublic(currentNpc);
            _characterCombat.AddDamageToAttackerPublic(currentNpc, 10);

            Assert.IsTrue(store.Remove(staleNpc));
            Assert.IsTrue(await store.AddAsync(replacementNpc));
            Assert.AreEqual(staleNpc.Index, replacementNpc.Index);

            Assert.AreSame(currentNpc, _characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task GetKiller_SkipsStaleFamiliarHandleAndDoesNotAttributeReplacementNpc()
        {
            var store = new NpcStore();
            using var provider = new ServiceCollection()
                .AddSingleton<INpcStore>(store)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var familiar = Substitute.For<INpc>();
            var replacement = Substitute.For<INpc>();
            Assert.IsTrue(await store.AddAsync(familiar));

            _characterCombat.AddAttackerPublic(familiar);
            _characterCombat.AddDamageToAttackerPublic(familiar, 25);

            Assert.IsTrue(store.Remove(familiar));
            Assert.IsTrue(await store.AddAsync(replacement));
            Assert.AreEqual(familiar.Index, replacement.Index);

            Assert.IsNull(_characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task DamageHistory_ExpiresAfterFiveMinutesWithoutResolvingEveryTick()
        {
            var store = CreateCharacterStore();
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(store)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);
            var attacker = Substitute.For<ICharacter>();
            attacker.MasterId.Returns(13u);
            Assert.IsTrue(await store.AddAsync(attacker));

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
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(store)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);
            var target = Substitute.For<ICharacter>();
            target.MasterId.Returns(14u);
            Assert.IsTrue(await store.AddAsync(target));
            Assert.IsTrue(_characterCombat.SetTargetReferenceForTest(target));

            Assert.IsTrue(store.Remove(target));

            Assert.IsFalse(_characterCombat.CanSetTarget(target));
        }

        [TestMethod]
        public async Task CanSetTarget_WhenNpcTargetWasRemoved_ReturnsFalse()
        {
            var store = new NpcStore();
            using var provider = new ServiceCollection()
                .AddSingleton<INpcStore>(store)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);
            var target = Substitute.For<INpc>();
            Assert.IsTrue(await store.AddAsync(target));
            Assert.IsTrue(_characterCombat.SetTargetReferenceForTest(target));

            Assert.IsTrue(store.Remove(target));

            Assert.IsFalse(_characterCombat.CanSetTarget(target));
        }

        [TestMethod]
        public async Task CanSetTarget_WhenCharacterSlotIsReused_DoesNotAcceptOriginalTarget()
        {
            var store = CreateCharacterStore();
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(store)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);
            var target = Substitute.For<ICharacter>();
            var replacement = Substitute.For<ICharacter>();
            target.MasterId.Returns(15u);
            replacement.MasterId.Returns(16u);
            Assert.IsTrue(await store.AddAsync(target));
            Assert.IsTrue(_characterCombat.SetTargetReferenceForTest(target));

            Assert.IsTrue(store.Remove(target));
            Assert.IsTrue(await store.AddAsync(replacement));
            Assert.AreEqual(target.Index, replacement.Index);

            Assert.IsFalse(_characterCombat.CanSetTarget(target));
            Assert.AreSame(target, _characterCombat.Target);
        }

        [TestMethod]
        public async Task CanSetTarget_WhenNpcSlotIsReused_DoesNotAcceptOriginalTarget()
        {
            var store = new NpcStore();
            using var provider = new ServiceCollection()
                .AddSingleton<INpcStore>(store)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);
            var target = Substitute.For<INpc>();
            var replacement = Substitute.For<INpc>();
            Assert.IsTrue(await store.AddAsync(target));
            Assert.IsTrue(_characterCombat.SetTargetReferenceForTest(target));

            Assert.IsTrue(store.Remove(target));
            Assert.IsTrue(await store.AddAsync(replacement));
            Assert.AreEqual(target.Index, replacement.Index);

            Assert.IsFalse(_characterCombat.CanSetTarget(target));
            Assert.AreSame(target, _characterCombat.Target);
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
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(store)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var attacker = Substitute.For<ICharacter>();
            Assert.IsTrue(await store.AddAsync(attacker));
            _characterCombat.AddAttackerPublic(attacker);
            _characterCombat.AddDamageToAttackerPublic(attacker, 10);
            _visibleCreatures.Clear();

            Assert.AreSame(attacker, _characterCombat.GetKiller());
        }

        [TestMethod]
        public async Task DamageHistory_StoresTypedHandlesInsteadOfCreatureReferences()
        {
            var characterStore = CreateCharacterStore();
            var npcStore = new NpcStore();
            using var provider = new ServiceCollection()
                .AddSingleton<ICharacterStore>(characterStore)
                .AddSingleton<INpcStore>(npcStore)
                .BuildServiceProvider();
            _mockOwner.ServiceProvider.Returns(provider);

            var character = Substitute.For<ICharacter>();
            var npc = Substitute.For<INpc>();
            Assert.IsTrue(await characterStore.AddAsync(character));
            Assert.IsTrue(await npcStore.AddAsync(npc));

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
                    Assert.IsTrue(attackerField!.FieldType.IsGenericType);
                    Assert.AreEqual(typeof(CreatureHandle<>), attackerField.FieldType.GetGenericTypeDefinition());
                    Assert.IsFalse(typeof(ICreature).IsAssignableFrom(attackerField.FieldType));
                }
            }
        }

        private static CharacterStore CreateCharacterStore() => new(Options.Create(new GameServerOptions
        {
            ClientRevision = 1,
            ClientRevisionPatch = 0,
            AuthenticationToken = "test"
        }));
    }

    public class TestableCharacterCombat : CharacterCombat
    {
        public TestableCharacterCombat(
            ICharacter owner,
            IAnimationBuilder animationBuilder,
            IGraphicBuilder graphicBuilder,
            IProjectileBuilder projectileBuilder,
            IMapRegionService mapRegionService,
            IGroundItemBuilder groundItemBuilder,
            IHitSplatBuilder hitSplatBuilder,
            IProjectilePathFinder projectilePathFinder,
            ISmartPathFinder smartPathFinder,
            IOptions<CombatOptions> combatOptions)
            : base(owner, animationBuilder, graphicBuilder, projectileBuilder, mapRegionService, groundItemBuilder,
                hitSplatBuilder, projectilePathFinder, smartPathFinder, combatOptions)
        {
        }

        public void SetDead(bool isDead)
        {
            IsDead = isDead;
        }

        public void SetTargetForTest(ICreature? target)
        {
            Target = target;
        }

        public bool SetTargetReferenceForTest(ICreature target)
        {
            return TrySetTargetReference(target);
        }

        public void AddAttackerPublic(ICreature attacker)
        {
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
