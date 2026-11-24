using System;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Combat.Magic;
using Hagalaz.Game.Scripts.Widgets.Tabs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Linq;

namespace Hagalaz.Game.Scripts.Tests.Widgets.Tabs;

[TestClass]
public sealed class SpellBookTabTests
{
    [TestMethod]
    public void ResolveThroughDependencyInjection_WithExplicitProjectileBuilder_DoesNotRequireGlobalInitialization()
    {
        using var provider = CreateProvider();

        var script = provider.GetRequiredService<SpellBookTab>();

        Assert.IsNotNull(script);
    }

    [TestMethod]
    [DataRow(20, typeof(AncientIceCombatSpell), 0)]
    [DataRow(21, typeof(AncientIceCombatSpell), 2)]
    [DataRow(22, typeof(AncientIceCombatSpell), 1)]
    [DataRow(23, typeof(AncientIceCombatSpell), 3)]
    [DataRow(24, typeof(AncientBloodCombatSpell), 0)]
    [DataRow(25, typeof(AncientBloodCombatSpell), 2)]
    [DataRow(26, typeof(AncientBloodCombatSpell), 1)]
    [DataRow(27, typeof(AncientBloodCombatSpell), 3)]
    [DataRow(28, typeof(AncientSmokeCombatSpell), 0)]
    [DataRow(29, typeof(AncientSmokeCombatSpell), 2)]
    [DataRow(30, typeof(AncientSmokeCombatSpell), 1)]
    [DataRow(31, typeof(AncientSmokeCombatSpell), 3)]
    [DataRow(32, typeof(AncientShadowCombatSpell), 0)]
    [DataRow(33, typeof(AncientShadowCombatSpell), 2)]
    [DataRow(34, typeof(AncientShadowCombatSpell), 1)]
    [DataRow(35, typeof(AncientShadowCombatSpell), 3)]
    [DataRow(36, typeof(AncientMiasmicCombatSpell), 0)]
    [DataRow(37, typeof(AncientMiasmicCombatSpell), 2)]
    [DataRow(38, typeof(AncientMiasmicCombatSpell), 1)]
    [DataRow(39, typeof(AncientMiasmicCombatSpell), 3)]
    public void CreateAncientSpells_MapsEachSpellbookButtonToItsFamilyAndSpellType(int buttonId, Type family, int spellType)
    {
        using var provider = CreateProvider();
        var spells = provider.GetRequiredService<SpellBookTab>().CreateAncientSpells(CreateDefinitions());

        var spell = (AncientCombatSpell)spells[buttonId];
        Assert.IsInstanceOfType(spell, family);
        Assert.AreEqual(spellType, spell.SpellType);
        Assert.AreEqual(buttonId, spell.Dto.ButtonId);
    }

    [TestMethod]
    public void CreateAncientSpells_IgnoresStandardBookDefinitionsSharingAButtonId()
    {
        using var provider = CreateProvider();
        var definitions = CreateDefinitions().Append(new CombatSpellDto { ButtonId = 25, SpellBook = MagicBook.StandardBook, RequiredLevel = 1 }).ToArray();

        var spells = provider.GetRequiredService<SpellBookTab>().CreateAncientSpells(definitions);

        Assert.HasCount(20, spells);
        Assert.AreEqual(MagicBook.AncientBook, ((AncientCombatSpell)spells[25]).Dto.SpellBook);
    }

    [TestMethod]
    public void CreateAncientSpells_LeavesAButtonWithoutAPersistedDefinitionUncastable()
    {
        using var provider = CreateProvider();
        var definitions = CreateDefinitions().Where(definition => definition.ButtonId != 31).ToArray();

        var spells = provider.GetRequiredService<SpellBookTab>().CreateAncientSpells(definitions);

        Assert.HasCount(19, spells);
        Assert.IsFalse(spells.ContainsKey(31));
    }

    private static CombatSpellDto[] CreateDefinitions() =>
        Enumerable.Range(20, 20).Select(buttonId => new CombatSpellDto { ButtonId = buttonId, SpellBook = MagicBook.AncientBook, RequiredLevel = buttonId }).ToArray();

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_ => Substitute.For<ICharacterContextAccessor>());
        services.AddSingleton(_ => Substitute.For<IScopedGameMediator>());
        services.AddSingleton(_ => Substitute.For<IMagicService>());
        services.AddSingleton(_ => Substitute.For<ICombatSpellCatalog>());
        services.AddSingleton(_ => Substitute.For<IItemBuilder>());
        services.AddSingleton(_ => Substitute.For<IProjectileBuilder>());
        services.AddTransient<SpellBookTab>();
        return services.BuildServiceProvider();
    }
}
