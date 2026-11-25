using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Profiles;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class HerbloreMappingTests
{
    private static IMapper CreateMapper() =>
        new MapperConfiguration(expression => expression.AddProfile<SkillsProfile>(), LoggerFactory.Create(_ => { })).CreateMapper();

    [TestMethod]
    public void Definition_maps_every_column_to_the_potion()
    {
        var dto = CreateMapper().Map<PotionDto>(new SkillsHerblorePotionDefinition
        {
            PotionId = 15325,
            Kind = (byte)PotionKind.Standard,
            UnfinishedPotionId = 169,
            RequiredLevel = 92,
            Experience = 106.3m,
            PrimaryItemIds = "267",
            SecondaryItemIds = "12539",
            SecondaryItemCount = 5
        });

        Assert.AreEqual(15325, dto.PotionId);
        Assert.AreEqual(PotionKind.Standard, dto.Kind);
        Assert.AreEqual(169, dto.UnfinishedPotionId);
        Assert.AreEqual(92, dto.RequiredLevel);
        Assert.AreEqual(106.3, dto.Experience);
        CollectionAssert.AreEqual(new[] { 267 }, dto.PrimaryItemIds);
        CollectionAssert.AreEqual(new[] { 12539 }, dto.SecondaryItemIds);
        Assert.AreEqual(5, dto.SecondaryItemCount);
    }

    [TestMethod]
    public void Overload_without_primary_ingredients_maps_every_secondary_ingredient()
    {
        var dto = CreateMapper().Map<PotionDto>(new SkillsHerblorePotionDefinition
        {
            PotionId = 15333,
            Kind = (byte)PotionKind.Overload,
            UnfinishedPotionId = 269,
            RequiredLevel = 96,
            Experience = 1000,
            PrimaryItemIds = "",
            SecondaryItemIds = "15309,15313,15317,15321,15325",
            SecondaryItemCount = 1
        });

        Assert.AreEqual(PotionKind.Overload, dto.Kind);
        Assert.IsEmpty(dto.PrimaryItemIds);
        CollectionAssert.AreEqual(new[] { 15309, 15313, 15317, 15321, 15325 }, dto.SecondaryItemIds);
    }
}
