using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects;
using Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Saradomin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Minigames.Godwars;

[TestClass]
public sealed class RopeCallerRegressionTests
{
    [TestMethod]
    public void Hole_WithRopeConsumesItAndInstallsGodWarsHoleState()
    {
        var (character, inventory, rope, builder) = CreateCharacterWithRope();
        var script = new Hole(builder);
        script.Initialize(CreateGameObject());

        script.OnCharacterClickPerform(character, GameObjectClickType.Option1Click);

        Assert.AreEqual(0, inventory.Items.GetCountById(954));
        character.Received(1).AddState(Arg.Is<HasGodWarsHoleRopeState>(_ => true));
        character.DidNotReceive().AddState(Arg.Any<HasSaradominFirstRockRopeState>());
    }

    [TestMethod]
    public void FirstRockDown_WithRopeConsumesItAndInstallsSaradominState()
    {
        var (character, inventory, rope, builder) = CreateCharacterWithRope();
        var script = new FirstRockDown(builder);
        script.Initialize(CreateGameObject());

        script.OnCharacterClickPerform(character, GameObjectClickType.Option1Click);

        Assert.AreEqual(0, inventory.Items.GetCountById(954));
        character.Received(1).AddState(Arg.Is<HasSaradominFirstRockRopeState>(_ => true));
        character.DidNotReceive().AddState(Arg.Any<HasGodWarsHoleRopeState>());
    }

    [TestMethod]
    public void Hole_WhenHoleRopeStateExists_ContinuesTraversalWithoutConsumingAnotherRope()
    {
        var (character, inventory, _, builder) = CreateCharacterWithRope();
        character.HasState<HasGodWarsHoleRopeState>().Returns(true);
        var script = new Hole(builder);
        script.Initialize(CreateGameObject());

        script.OnCharacterClickPerform(character, GameObjectClickType.Option1Click);

        Assert.AreEqual(1, inventory.Items.GetCountById(954));
        character.DidNotReceive().AddState(Arg.Any<HasSaradominFirstRockRopeState>());
    }

    [TestMethod]
    public void FirstRockDown_WhenSaradominRopeStateExists_DoesNotConsumeAnotherRope()
    {
        var (character, inventory, _, builder) = CreateCharacterWithRope();
        character.HasState<HasSaradominFirstRockRopeState>().Returns(true);
        var script = new FirstRockDown(builder);
        script.Initialize(CreateGameObject());

        script.OnCharacterClickPerform(character, GameObjectClickType.Option2Click);

        Assert.AreEqual(1, inventory.Items.GetCountById(954));
        character.DidNotReceive().AddState(Arg.Any<HasGodWarsHoleRopeState>());
    }

    [TestMethod]
    public void Hole_WithoutRopeReportsFailureAndDoesNotInstallTraversalState()
    {
        var inventory = new ComposedTestContainer(2);
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        var script = new Hole(new TestItemBuilder(ComposedTestContainer.CreateTestItem));
        script.Initialize(CreateGameObject());

        script.OnCharacterClickPerform(character, GameObjectClickType.Option1Click);

        Assert.AreEqual(0, inventory.Items.TakenSlots);
        character.Received(1).SendChatMessage("You need a rope in order to climb down here.");
        character.DidNotReceive().AddState(Arg.Any<HasGodWarsHoleRopeState>());
        character.DidNotReceive().QueueTask(Arg.Any<Hagalaz.Game.Abstractions.Tasks.ITaskItem>());
    }

    private static (ICharacter Character, ComposedTestContainer Inventory, IItem Rope, IItemBuilder Builder)
        CreateCharacterWithRope()
    {
        var inventory = new ComposedTestContainer(2);
        var rope = ComposedTestContainer.CreateTestItem(954);
        inventory.Items.Add(rope);
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        var builder = new TestItemBuilder((id, count) => id == 954 ? rope : ComposedTestContainer.CreateTestItem(id, count));
        return (character, inventory, rope, builder);
    }

    private static IGameObject CreateGameObject()
    {
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.VarpBitFileId.Returns(123);
        var gameObject = Substitute.For<IGameObject>();
        gameObject.Definition.Returns(definition);
        return gameObject;
    }
}
