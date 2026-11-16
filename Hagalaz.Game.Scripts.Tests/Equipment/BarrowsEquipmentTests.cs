using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Equipment;
using Hagalaz.Game.Scripts.Equipment.Barrows;
using Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons;
using Hagalaz.Game.Scripts.Skills.Combat.Ranged.Crossbows;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Providers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Equipment;

/// <summary>
///     Characterizes the Barrows set detection and degradation rules. Expected item IDs are literal data taken from the
///     implementation-derived stage catalog and the original scripts, never computed by the code under test.
/// </summary>
[TestClass]
public sealed class BarrowsEquipmentTests
{
    private static readonly EquipmentSlot[] _partSlots = [EquipmentSlot.Hat, EquipmentSlot.Weapon, EquipmentSlot.Chest, EquipmentSlot.Legs];

    public sealed record Family(string Name, Type ScriptType, Type StateType, int[] Ids, int[] Successors, Type? WeaponScriptType = null);

    private static readonly Family[] _families =
    [
        new("Ahrim", typeof(AhrimEquipment), typeof(AhrimBlightState),
            [4708, 4856, 4857, 4858, 4859, 4710, 4862, 4863, 4864, 4865, 4712, 4868, 4869, 4870, 4871, 4714, 4874, 4875, 4876, 4877],
            [4856, 4857, 4858, 4859, 4860, 4862, 4863, 4864, 4865, 4866, 4868, 4869, 4870, 4871, 4872, 4874, 4875, 4876, 4877, 4878]),
        new("Akrisae", typeof(AkrisaeEquipment), typeof(AkrisaeDoomState),
            [21736, 21738, 21739, 21740, 21741, 21744, 21746, 21747, 21748, 21749, 21752, 21754, 21755, 21756, 21757, 21760, 21762, 21763, 21764, 21765],
            [21738, 21739, 21740, 21741, 21742, 21746, 21747, 21748, 21749, 21750, 21754, 21755, 21756, 21757, 21758, 21762, 21763, 21764, 21765, 21766]),
        new("Dharok", typeof(DharokEquipment), typeof(DharokWretchedStrengthState),
            [4716, 4880, 4881, 4882, 4883, 4718, 4886, 4887, 4888, 4889, 4720, 4892, 4893, 4894, 4895, 4722, 4898, 4899, 4900, 4901],
            [4880, 4881, 4882, 4883, 4884, 4886, 4887, 4888, 4889, 4890, 4892, 4893, 4894, 4895, 4896, 4898, 4899, 4900, 4901, 4902]),
        new("Guthan", typeof(GuthanEquipment), typeof(GuthanInfestationState),
            [4724, 4904, 4905, 4906, 4907, 4726, 4910, 4911, 4912, 4913, 4728, 4916, 4917, 4918, 4919, 4730, 4922, 4923, 4924, 4925],
            [4904, 4905, 4906, 4907, 4908, 4910, 4911, 4912, 4913, 4914, 4916, 4917, 4918, 4919, 4920, 4922, 4923, 4924, 4925, 4926],
            typeof(GuthansWarSpear)),
        new("Karil", typeof(KarilEquipment), typeof(KarilTaintState),
            [4732, 4928, 4929, 4930, 4931, 4734, 4934, 4935, 4936, 4937, 4736, 4940, 4941, 4942, 4943, 4738, 4946, 4947, 4948, 4949],
            [4928, 4929, 4930, 4931, 4932, 4934, 4935, 4936, 4937, 4938, 4940, 4941, 4942, 4943, 4944, 4946, 4947, 4948, 4949, 4950],
            typeof(KarilsCrossbow)),
        new("Torag", typeof(ToragEquipment), typeof(ToragCorruptState),
            [4745, 4952, 4953, 4954, 4955, 4747, 4958, 4959, 4960, 4961, 4749, 4964, 4965, 4966, 4967, 4751, 4970, 4971, 4972, 4973],
            [4952, 4953, 4954, 4955, 4956, 4958, 4959, 4960, 4961, 4962, 4964, 4965, 4966, 4967, 4968, 4970, 4971, 4972, 4973, 4974]),
        new("Verac", typeof(VeracEquipment), typeof(VeracDefileState),
            [4753, 4976, 4977, 4978, 4979, 4755, 4982, 4983, 4984, 4985, 4757, 4988, 4989, 4990, 4991, 4759, 4994, 4995, 4996, 4997],
            [4976, 4977, 4978, 4979, 4980, 4982, 4983, 4984, 4985, 4986, 4988, 4989, 4990, 4991, 4992, 4994, 4995, 4996, 4997, 4998])
    ];

    public static IEnumerable<object[]> Families => _families.Select(family => new object[] { family });

    private static readonly Lazy<(IServiceProvider Services, Dictionary<int, List<Type>> Handlers)> _registration = new(CreateRegistration);

    [TestMethod]
    [DynamicData(nameof(Families))]
    public void DegradationStages_MatchCharacterizedIds(Family family)
    {
        for (var index = 0; index < family.Ids.Length; index++)
        {
            var item = Item(family.Ids[index]);
            var script = (DegradingEquipment)CreateHandler(family.Ids[index]);
            var initialStage = index % 5 == 0;

            Assert.AreEqual(initialStage ? 0 : 22500, script.GetDegrationTicks(item), $"{family.Name} ticks for {item.Id}");
            Assert.AreEqual(0.80, script.GetDegrationOnDeathFactor(item), $"{family.Name} death factor for {item.Id}");
            Assert.AreEqual(family.Successors[index], script.GetDegradedItemID(item), $"{family.Name} successor of {item.Id}");
        }
    }

    [TestMethod]
    [DynamicData(nameof(Families))]
    public void EveryDeclaredId_ResolvesToExactlyOneHandler(Family family)
    {
        for (var index = 0; index < family.Ids.Length; index++)
        {
            var id = family.Ids[index];
            var handlers = _registration.Value.Handlers.GetValueOrDefault(id) ?? [];
            var expected = index / 5 == 1 && family.WeaponScriptType is not null ? family.WeaponScriptType : family.ScriptType;

            Assert.HasCount(1, handlers, $"{family.Name} handlers for {id}");
            Assert.AreEqual(expected, handlers[0], $"{family.Name} handler for {id}");
        }
    }

    [TestMethod]
    [DynamicData(nameof(Families))]
    public void CompleteSet_ActivatesOwnStateOnly(Family family)
    {
        foreach (var stages in new[] { new[] { 0, 0, 0, 0 }, new[] { 4, 2, 1, 3 }, new[] { 3, 4, 0, 2 } })
        {
            var world = new World();
            world.Equip(family, stages);

            Equip(world, family.Ids[stages[1] + 5]);

            AssertOnlyFamilyState(family, world.AddedStates, $"{family.Name} stages {string.Join(',', stages)}");
        }
    }

    [TestMethod]
    [DynamicData(nameof(Families))]
    public void EachSlotCompletingTheSet_ActivatesOwnState(Family family)
    {
        for (var part = 0; part < 4; part++)
        {
            var world = new World();
            world.Equip(family, [0, 0, 0, 0]);

            Equip(world, family.Ids[part * 5]);

            AssertOnlyFamilyState(family, world.AddedStates, $"{family.Name} last slot {_partSlots[part]}");
        }
    }

    [TestMethod]
    [DynamicData(nameof(Families))]
    public void MissingPiece_DoesNotActivateState(Family family)
    {
        for (var missing = 0; missing < 4; missing++)
        {
            var world = new World();
            world.Equip(family, [0, 0, 0, 0]);
            world.Unequip(_partSlots[missing]);

            Equip(world, family.Ids[(missing + 1) % 4 * 5]);

            Assert.IsEmpty(world.AddedStates.Where(state => state.GetType() == family.StateType), $"{family.Name} missing {_partSlots[missing]}");
        }
    }

    [TestMethod]
    [DynamicData(nameof(Families))]
    public void MismatchedFamilyOrUnrelatedPiece_DoesNotActivateState(Family family)
    {
        var other = _families.First(candidate => candidate != family);
        for (var part = 0; part < 4; part++)
        {
            foreach (var replacement in new[] { other.Ids[part * 5], 1 })
            {
                var world = new World();
                world.Equip(family, [0, 0, 0, 0]);
                world.Equip(_partSlots[part], replacement);

                Equip(world, family.Ids[(part + 1) % 4 * 5]);

                Assert.IsEmpty(world.AddedStates.Where(state => state.GetType() == family.StateType),
                    $"{family.Name} slot {_partSlots[part]} replaced with {replacement}");
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(Families))]
    public void Unequip_RemovesOwnStateForEveryLastEquippedSlot(Family family)
    {
        for (var part = 0; part < 4; part++)
        {
            var world = new World();
            world.Equip(family, [0, 0, 0, 0]);
            var id = family.Ids[part * 5];

            CreateHandler(id).OnUnequipped(Item(id), world.Character);

            AssertRemoved(family.StateType, world.Character);
        }
    }

    private static void Equip(World world, int lastEquippedId) =>
        CreateHandler(lastEquippedId).OnEquipped(Item(lastEquippedId), world.Character);

    private static void AssertOnlyFamilyState(Family family, List<IState> added, string message)
    {
        Assert.HasCount(1, added.Where(state => state.GetType() == family.StateType), message);
        Assert.IsFalse(added.Any(state => state.GetType() != family.StateType && state is not CrossbowEquippedState), message);
    }

    private static void AssertRemoved(Type stateType, ICharacter character)
    {
        var removed = character.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ICharacter.RemoveState))
            .Select(call => call.GetMethodInfo().GetGenericArguments()[0])
            .ToList();
        CollectionAssert.Contains(removed, stateType);
    }

    private static IItem Item(int id)
    {
        var item = Substitute.For<IItem>();
        item.Id.Returns(id);
        return item;
    }

    private static IEquipmentScript CreateHandler(int id) =>
        (IEquipmentScript)ActivatorUtilities.CreateInstance(_registration.Value.Services, _registration.Value.Handlers[id].Single());

    private static (IServiceProvider, Dictionary<int, List<Type>>) CreateRegistration()
    {
        var services = new ServiceCollection();
        new Startup().Configure(services);
        var factory = new EquipmentScriptMetaDataFactory(new ServiceDescriptorProvider(services));
        var handlers = new Dictionary<int, List<Type>>();
        foreach (var (itemId, scriptType) in factory.GetScripts().ToBlockingEnumerable())
        {
            if (!handlers.TryGetValue(itemId, out var types))
            {
                handlers[itemId] = types = [];
            }

            types.Add(scriptType);
        }

        var provider = new ServiceCollection().AddSingleton(Substitute.For<ICrossbowLogicService>()).BuildServiceProvider();
        return (provider, handlers);
    }

    private sealed class World
    {
        private readonly Dictionary<EquipmentSlot, IItem> _equipped = [];

        public ICharacter Character { get; } = Substitute.For<ICharacter>();
        public List<IState> AddedStates { get; } = [];

        public World()
        {
            var equipment = Substitute.For<IEquipmentContainer>();
            equipment[Arg.Any<EquipmentSlot>()].Returns(call => _equipped.GetValueOrDefault(call.Arg<EquipmentSlot>()));
            Character.Equipment.Returns(equipment);
            Character.When(character => character.AddState(Arg.Any<IState>())).Do(call => AddedStates.Add((IState)call[0]!));
        }

        public void Equip(Family family, int[] stages)
        {
            for (var part = 0; part < 4; part++)
            {
                Equip(_partSlots[part], family.Ids[part * 5 + stages[part]]);
            }
        }

        public void Equip(EquipmentSlot slot, int id) => _equipped[slot] = BarrowsEquipmentTests.Item(id);

        public void Unequip(EquipmentSlot slot) => _equipped.Remove(slot);
    }
}
