using System.Globalization;
using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Profiles;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class FletchingMappingTests
{
    private static IMapper CreateMapper() =>
        new MapperConfiguration(expression => expression.AddProfile<SkillsProfile>(), LoggerFactory.Create(_ => { })).CreateMapper();

    [TestMethod]
    public void Definition_maps_every_column_to_the_recipe()
    {
        var dto = CreateMapper().Map<FletchingDto>(new SkillsFletchingDefinition
        {
            ResourceId = 1511,
            ToolId = 946,
            Category = (byte)FletchingCategory.Wood,
            ProductIds = "52,50,48,9440",
            ProductCounts = "15,1,1,1",
            ProductExperiences = "0.33,5,10,6",
            RequiredLevels = "1,5,10,9",
            AnimationId = 6702
        });

        Assert.AreEqual(1511, dto.ResourceId);
        Assert.AreEqual(946, dto.ToolId);
        Assert.AreEqual(FletchingCategory.Wood, dto.Category);
        CollectionAssert.AreEqual(new[] { 52, 50, 48, 9440 }, dto.ProductIds);
        CollectionAssert.AreEqual(new[] { 15, 1, 1, 1 }, dto.ProductCounts);
        CollectionAssert.AreEqual(new[] { 0.33, 5d, 10d, 6d }, dto.ProductExperiences);
        CollectionAssert.AreEqual(new[] { 1, 5, 10, 9 }, dto.RequiredLevels);
        Assert.AreEqual(6702, dto.AnimationId);
    }

    [TestMethod]
    public void Definition_without_animation_maps_to_minus_one()
    {
        var dto = CreateMapper().Map<FletchingDto>(NewDefinition("6", animationId: null));

        Assert.AreEqual(-1, dto.AnimationId);
    }

    [TestMethod]
    public void Experience_parses_with_the_invariant_culture_under_a_comma_decimal_locale()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("nl-NL");
        try
        {
            var dto = CreateMapper().Map<FletchingDto>(NewDefinition("0.33,83.25,91.5"));

            CollectionAssert.AreEqual(new[] { 0.33, 83.25, 91.5 }, dto.ProductExperiences);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private static SkillsFletchingDefinition NewDefinition(string experiences, ushort? animationId = 6702) =>
        new()
        {
            ResourceId = 1,
            ToolId = 2,
            Category = (byte)FletchingCategory.Bow,
            ProductIds = "3,4,5",
            ProductCounts = "1,1,1",
            ProductExperiences = experiences,
            RequiredLevels = "1,1,1",
            AnimationId = animationId
        };
}
