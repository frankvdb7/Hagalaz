using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Dialogues.Generic;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Crafting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Crafting;

[TestClass]
public sealed class CraftingProductNamingTests
{
    [TestMethod]
    public async Task SpinProductName_UsesPlainAndRedMarkupForRequirementState()
    {
        var inventory = new ComposedTestContainer(2);
        var resource = ComposedTestContainer.CreateTestItem(300);
        inventory.SetItem(0, resource);
        var dto = new SpinDto { ResourceID = 300, ProductID = 400, RequiredLevel = 10, CraftingExperience = 5 };
        var service = Substitute.For<ICraftingService>();
        service.FindAllSpin().Returns(Task.FromResult<IReadOnlyList<SpinDto>>([dto]));
        service.FindSpinByProductID(400).Returns(Task.FromResult<SpinDto?>(dto));
        var itemService = Substitute.For<IItemService>();
        var productDefinition = CreateDefinition("Bow string");
        var resourceDefinition = CreateDefinition("Flax");
        itemService.FindItemDefinitionById(400).Returns(productDefinition);
        itemService.FindItemDefinitionById(300).Returns(resourceDefinition);
        var (character, dialogue) = CreateCharacter(inventory, service, itemService, craftingLevel: 10);

        await new CraftingSkillService(CreateItemBuilder()).TrySpin(character);

        Assert.AreEqual("Bow string<br>(Flax)", dialogue.ProductNamingCallback(400));
        inventory.Items.Remove(resource, 0);
        character.Statistics.GetSkillLevel(StatisticsConstants.Crafting).Returns(1);
        Assert.AreEqual("<col=FF0000>Bow string</col><br>(Flax)", dialogue.ProductNamingCallback(400));
    }

    [TestMethod]
    public async Task SpinProductName_WhenDefinitionLookupReturnsNull_ReturnsEmptyName()
    {
        var service = Substitute.For<ICraftingService>();
        service.FindAllSpin().Returns(Task.FromResult<IReadOnlyList<SpinDto>>([new SpinDto
        {
            ResourceID = 300,
            ProductID = 400,
            RequiredLevel = 1,
            CraftingExperience = 1
        }]));
        service.FindSpinByProductID(400).Returns(Task.FromResult<SpinDto?>(null));
        var (character, dialogue) = CreateCharacter(new ComposedTestContainer(1), service,
            Substitute.For<IItemService>(), craftingLevel: 1);

        await new CraftingSkillService(CreateItemBuilder()).TrySpin(character);

        Assert.AreEqual(string.Empty, dialogue.ProductNamingCallback(400));
    }

    private static (ICharacter Character, InteractiveDialogueScript Dialogue) CreateCharacter(
        ComposedTestContainer inventory, ICraftingService craftingService, IItemService itemService, int craftingLevel)
    {
        var context = Substitute.For<ICharacterContext>();
        var character = Substitute.For<ICharacter>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        var statistics = Substitute.For<ICharacterStatistics>();
        statistics.GetSkillLevel(StatisticsConstants.Crafting).Returns(craftingLevel);
        character.Inventory.Returns(inventory);
        character.Statistics.Returns(statistics);
        character.Widgets.Returns(Substitute.For<IWidgetContainer>());
        var dialogue = new InteractiveDialogueScript(accessor, itemService, Substitute.For<IWidgetOptionBuilder>());
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(ICraftingService)).Returns(craftingService);
        provider.GetService(typeof(IItemService)).Returns(itemService);
        provider.GetService(typeof(InteractiveDialogueScript)).Returns(dialogue);
        provider.GetService(typeof(DefaultDialogueScript)).Returns(new DefaultDialogueScript(accessor));
        character.ServiceProvider.Returns(provider);
        return (character, dialogue);
    }

    private static IItemDefinition CreateDefinition(string name)
    {
        var definition = Substitute.For<IItemDefinition>();
        definition.Name.Returns(name);
        return definition;
    }

    private static Hagalaz.Game.Abstractions.Builders.Item.IItemBuilder CreateItemBuilder() =>
        new TestItemBuilder(ComposedTestContainer.CreateTestItem);
}
