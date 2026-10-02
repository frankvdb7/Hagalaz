using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Prayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Prayer;

[TestClass]
public sealed class PrayerLookupTests
{
    [TestMethod]
    public async Task FindAvailableItem_ReturnsDefinitionAndExactSlotOnlyForCurrentInstance()
    {
        var items = new ItemContainer(StorageType.Normal, 2);
        var item = ComposedTestContainer.CreateTestItem(526);
        items.Add(item);
        var inventory = Substitute.For<IInventoryContainer>();
        inventory.Items.Returns(items);
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        var prayerService = Substitute.For<IPrayerService>();
        var definition = new PrayerDto { ItemId = 526, Experience = 4.5, Type = PrayerDtoType.Bones };
        prayerService.FindById(526).Returns(Task.FromResult<PrayerDto?>(definition));

        var result = await Hagalaz.Game.Scripts.Skills.Prayer.Prayer.FindAvailableItem(character, item, prayerService, CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreSame(definition, result.Value.Definition);
        Assert.AreEqual(0, result.Value.Slot);
    }

    [TestMethod]
    public async Task FindAvailableItem_WhenDefinitionIsMissing_ReturnsNull()
    {
        var character = CreateCharacter(new ItemContainer(StorageType.Normal, 1));
        var item = ComposedTestContainer.CreateTestItem(526);
        var prayerService = Substitute.For<IPrayerService>();
        prayerService.FindById(526).Returns(Task.FromResult<PrayerDto?>(null));

        var result = await Hagalaz.Game.Scripts.Skills.Prayer.Prayer.FindAvailableItem(character, item, prayerService, CancellationToken.None);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task FindAvailableItem_WhenExactItemWasRemoved_ReturnsNull()
    {
        var items = new ItemContainer(StorageType.Normal, 1);
        var character = CreateCharacter(items);
        var item = ComposedTestContainer.CreateTestItem(526);
        var prayerService = Substitute.For<IPrayerService>();
        prayerService.FindById(526).Returns(Task.FromResult<PrayerDto?>(new PrayerDto
        {
            ItemId = 526,
            Experience = 4.5,
            Type = PrayerDtoType.Bones
        }));

        var result = await Hagalaz.Game.Scripts.Skills.Prayer.Prayer.FindAvailableItem(character, item, prayerService, CancellationToken.None);

        Assert.IsNull(result);
    }

    private static ICharacter CreateCharacter(ItemContainer items)
    {
        var inventory = Substitute.For<IInventoryContainer>();
        inventory.Items.Returns(items);
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        return character;
    }
}
