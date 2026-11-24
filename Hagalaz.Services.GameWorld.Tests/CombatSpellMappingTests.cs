using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Profiles;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CombatSpellMappingTests
{
    private static IMapper CreateMapper() =>
        new MapperConfiguration(expression => expression.AddProfile<SkillsProfile>(), LoggerFactory.Create(_ => { })).CreateMapper();

    [TestMethod]
    [DataRow("StandartBook", MagicBook.StandardBook)]
    [DataRow("AncientBook", MagicBook.AncientBook)]
    public void Combat_definition_maps_the_spellbook(string persisted, MagicBook expected)
    {
        var dto = CreateMapper().Map<CombatSpellDto>(Create(persisted));

        Assert.AreEqual(expected, dto.SpellBook);
    }

    [TestMethod]
    public void Combat_definition_maps_requirements_experience_damage_and_autocast_config()
    {
        var dto = CreateMapper().Map<CombatSpellDto>(Create("AncientBook"));

        Assert.AreEqual(25, dto.ButtonId);
        Assert.AreEqual(80, dto.RequiredLevel);
        Assert.AreEqual(45d, dto.BaseExperience);
        Assert.AreEqual(250, dto.BaseDamage);
        Assert.AreEqual(83, dto.AutoCastConfig);
        CollectionAssert.AreEqual(new[] { RuneType.Death, RuneType.Blood }, dto.RequiredRunes);
        CollectionAssert.AreEqual(new[] { 2, 4 }, dto.RequiredRunesCounts);
    }

    private static SkillsMagicCombatDefinition Create(string spellBook) => new()
    {
        ButtonId = 25,
        SpellBook = spellBook,
        RequiredLevel = 80,
        BaseExperience = 45m,
        BaseDamage = 250,
        AutocastConfig = 83,
        RequiredRunes = "560,565",
        RequiredRunesCounts = "2,4"
    };
}
