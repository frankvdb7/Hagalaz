using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Model.Creatures;
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

namespace Hagalaz.Game.Scripts.Tests.Widgets.Tabs;

[TestClass]
public sealed class SpellBookTabTests
{
    [TestMethod]
    [DataRow(MagicBook.StandardBook, ComponentClickType.Option6Click)]
    [DataRow(MagicBook.AncientBook, ComponentClickType.LeftClick)]
    public void OnOpen_AutocastHandlerRejectsTheOtherBooksClickType(MagicBook book, ComponentClickType clickType)
    {
        var fixture = OpenSpellBook(book);
        const int sharedButton = 25;

        Assert.IsFalse(fixture.ClickHandlers[sharedButton](sharedButton, clickType, 41, 42));

        Assert.IsFalse(fixture.Character.Magic.ReceivedCalls()
            .Any(call => call.GetMethodInfo().Name == nameof(IMagic.SetAutoCastingSpell)));
    }

    [TestMethod]
    public void ResolveThroughDependencyInjection_WithExplicitProjectileBuilder_DoesNotRequireGlobalInitialization()
    {
        using var provider = CreateProvider();

        var script = provider.GetRequiredService<SpellBookTab>();

        Assert.IsNotNull(script);
    }

    [TestMethod]
    [DynamicData(nameof(AncientButtons))]
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

    /// <summary>
    ///     Each Ancient spellbook button with the family and spell type it casts: a family occupies four consecutive buttons laid out as rush, blitz, burst, barrage.
    /// </summary>
    public static IEnumerable<object[]> AncientButtons
    {
        get
        {
            var families = new[] { typeof(AncientIceCombatSpell), typeof(AncientBloodCombatSpell), typeof(AncientSmokeCombatSpell), typeof(AncientShadowCombatSpell), typeof(AncientMiasmicCombatSpell) };
            int[] typeByOffset = [0, 2, 1, 3];
            for (var family = 0; family < families.Length; family++)
            {
                for (var offset = 0; offset < typeByOffset.Length; offset++)
                {
                    yield return [20 + family * 4 + offset, families[family], typeByOffset[offset]];
                }
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(AncientButtons))]
    public void OnOpen_AncientBook_AutocastAndCastHandlersSelectTheirOwnRow(int buttonId, Type family, int spellType)
    {
        var fixture = OpenSpellBook(MagicBook.AncientBook);

        Assert.IsTrue(fixture.ClickHandlers[buttonId](buttonId, ComponentClickType.Option6Click, 0, 0));
        var autocast = (AncientCombatSpell)fixture.Character.Magic.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IMagic.SetAutoCastingSpell)).GetArguments()[0]!;
        AssertAncientRow(autocast, buttonId, family, spellType);

        Assert.IsTrue(fixture.UseOnCreatureHandlers[buttonId](buttonId, Substitute.For<ICreature>(), false, 0, 0));
        AssertAncientRow((AncientCombatSpell)fixture.Character.Magic.SelectedSpell!, buttonId, family, spellType);
    }

    [TestMethod]
    [DataRow(25)]
    [DataRow(28)]
    [DataRow(39)]
    public void OnOpen_StandardBook_HandlersSelectTheStandardRowNotTheCollidingAncientOne(int buttonId)
    {
        var fixture = OpenSpellBook(MagicBook.StandardBook);

        Assert.IsTrue(fixture.ClickHandlers[buttonId](buttonId, ComponentClickType.LeftClick, 0, 0));
        var autocast = (StandardCombatSpell)fixture.Character.Magic.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IMagic.SetAutoCastingSpell)).GetArguments()[0]!;
        Assert.AreEqual(StandardRowLevel(buttonId), autocast.Dto.RequiredLevel);
        Assert.AreEqual(MagicBook.StandardBook, autocast.Dto.SpellBook);

        Assert.IsTrue(fixture.UseOnCreatureHandlers[buttonId](buttonId, Substitute.For<ICreature>(), false, 0, 0));
        Assert.AreEqual(StandardRowLevel(buttonId), ((StandardCombatSpell)fixture.Character.Magic.SelectedSpell!).Dto.RequiredLevel);
    }

    private static int AncientRowLevel(int buttonId) => 1000 + buttonId;

    private static int StandardRowLevel(int buttonId) => 2000 + buttonId;

    private static void AssertAncientRow(AncientCombatSpell spell, int buttonId, Type family, int spellType)
    {
        Assert.IsInstanceOfType(spell, family);
        Assert.AreEqual(spellType, spell.SpellType);
        Assert.AreEqual(AncientRowLevel(buttonId), spell.Dto.RequiredLevel);
        Assert.AreEqual(MagicBook.AncientBook, spell.Dto.SpellBook);
    }

    private sealed record OpenedSpellBook(
        ICharacter Character, Dictionary<int, OnComponentClick> ClickHandlers, Dictionary<int, OnComponentUsedOnCreature> UseOnCreatureHandlers);

    /// <summary>
    ///     Opens the real tab for a character on the given book with Ancient rows 20-39 and standard rows that collide on some of those buttons.
    ///     Each row carries a distinct level so the test can tell which row a handler selected.
    /// </summary>
    private static OpenedSpellBook OpenSpellBook(MagicBook book)
    {
        var character = Substitute.For<ICharacter>();
        character.Profile.GetValue<MagicBook>(Arg.Any<string>(), Arg.Any<MagicBook>()).Returns(book);
        character.Magic.CheckMagicLevel(Arg.Any<int>()).Returns(true);
        character.Magic.CheckRunes(Arg.Any<RuneType[]>(), Arg.Any<int[]>()).Returns(true);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Character.Returns(character);

        var definitions = Enumerable.Range(20, 20)
            .Select(buttonId => CreateDefinition(buttonId, MagicBook.AncientBook, AncientRowLevel(buttonId)))
            .Concat(new[] { 25, 28, 30, 32, 34, 36, 39 }.Select(buttonId => CreateDefinition(buttonId, MagicBook.StandardBook, StandardRowLevel(buttonId))))
            .ToArray();
        var catalog = Substitute.For<ICombatSpellCatalog>();
        catalog.CombatSpells.Returns(definitions);

        var clickHandlers = new Dictionary<int, OnComponentClick>();
        var useOnCreatureHandlers = new Dictionary<int, OnComponentUsedOnCreature>();
        var widget = Substitute.For<IWidget>();
        widget.When(w => w.AttachClickHandler(Arg.Any<int>(), Arg.Any<OnComponentClick>())).Do(call => clickHandlers[call.ArgAt<int>(0)] = call.ArgAt<OnComponentClick>(1));
        widget.When(w => w.AttachUseOnCreatureHandler(Arg.Any<int>(), Arg.Any<OnComponentUsedOnCreature>()))
            .Do(call => useOnCreatureHandlers[call.ArgAt<int>(0)] = call.ArgAt<OnComponentUsedOnCreature>(1));

        using var provider = CreateProvider(accessor, catalog);
        var tab = provider.GetRequiredService<SpellBookTab>();
        tab.Initialize(widget);
        tab.OnOpen();
        return new OpenedSpellBook(character, clickHandlers, useOnCreatureHandlers);
    }

    private static CombatSpellDto CreateDefinition(int buttonId, MagicBook book, int level) => new()
    {
        ButtonId = buttonId,
        SpellBook = book,
        RequiredLevel = level,
        RequiredRunes = [],
        RequiredRunesCounts = []
    };

    private static CombatSpellDto[] CreateDefinitions() =>
        Enumerable.Range(20, 20).Select(buttonId => new CombatSpellDto { ButtonId = buttonId, SpellBook = MagicBook.AncientBook, RequiredLevel = buttonId }).ToArray();

    private static ServiceProvider CreateProvider(ICharacterContextAccessor? accessor = null, ICombatSpellCatalog? catalog = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_ => accessor ?? Substitute.For<ICharacterContextAccessor>());
        services.AddSingleton(_ => Substitute.For<IScopedGameMediator>());
        services.AddSingleton(_ => Substitute.For<IMagicService>());
        services.AddSingleton(_ => catalog ?? Substitute.For<ICombatSpellCatalog>());
        services.AddSingleton(_ => Substitute.For<IItemBuilder>());
        services.AddSingleton(_ => Substitute.For<IProjectileBuilder>());
        services.AddTransient<SpellBookTab>();
        return services.BuildServiceProvider();
    }
}
