using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Items.Godwars;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Items;

[TestClass]
public sealed class GodSwordCompositionTests
{
    [DataTestMethod]
    [DataRow(11702, 11690, 11694)]
    [DataRow(11690, 11702, 11694)]
    [DataRow(11708, 11690, 11700)]
    [DataRow(11690, 11708, 11700)]
    [DataRow(11706, 11690, 11698)]
    [DataRow(11690, 11706, 11698)]
    [DataRow(11704, 11690, 11696)]
    [DataRow(11690, 11704, 11696)]
    public void HiltAndGodSword_ProduceExpectedSwordInEitherArgumentOrder(int usedId, int usedWithId, int resultId)
    {
        var (character, inventory, used, usedWith) = CreateScenario(usedId, usedWithId);

        var succeeded = new GodSwordHilt(CreateItemBuilder()).UseItemOnItem(used, usedWith, character);

        Assert.IsTrue(succeeded);
        Assert.AreEqual(resultId, inventory.Items[1]!.Id);
        Assert.IsNull(inventory.Items[0]);
    }

    [DataTestMethod]
    [DataRow(11710, 11692, 11690)]
    [DataRow(11692, 11710, 11690)]
    [DataRow(11712, 11688, 11690)]
    [DataRow(11688, 11712, 11690)]
    [DataRow(11714, 11686, 11690)]
    [DataRow(11686, 11714, 11690)]
    [DataRow(11712, 11714, 11692)]
    [DataRow(11714, 11712, 11692)]
    [DataRow(11710, 11712, 11686)]
    [DataRow(11712, 11710, 11686)]
    [DataRow(11710, 11714, 11688)]
    public void SupportedShardPairs_AssembleExpectedSword(int usedId, int usedWithId, int resultId)
    {
        var (character, inventory, used, usedWith) = CreateScenario(usedId, usedWithId);

        var succeeded = new GodSwordShard(CreateItemBuilder()).UseItemOnItem(used, usedWith, character);

        Assert.IsTrue(succeeded);
        Assert.AreEqual(resultId, inventory.Items[1]!.Id);
        Assert.AreEqual(1, inventory.Items.TakenSlots);
    }

    [TestMethod]
    public void Shard11710And11714_ReverseArgumentOrderIsUnsupportedAndDoesNotMutate()
    {
        var (character, inventory, used, usedWith) = CreateScenario(11714, 11710);

        var succeeded = new GodSwordShard(CreateItemBuilder()).UseItemOnItem(used, usedWith, character);

        Assert.IsFalse(succeeded);
        Assert.AreSame(used, inventory.Items[0]);
        Assert.AreSame(usedWith, inventory.Items[1]);
    }

    private static (ICharacter Character, ComposedTestContainer Inventory, IItem Used, IItem UsedWith)
        CreateScenario(int usedId, int usedWithId)
    {
        var inventory = new ComposedTestContainer(4);
        var used = ComposedTestContainer.CreateTestItem(usedId);
        var usedWith = ComposedTestContainer.CreateTestItem(usedWithId);
        inventory.Items.Add(used);
        inventory.Items.Add(usedWith);
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        return (character, inventory, used, usedWith);
    }

    private static IItemBuilder CreateItemBuilder() => new TestItemBuilder(ComposedTestContainer.CreateTestItem);
}
