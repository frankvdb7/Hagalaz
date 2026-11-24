using AutoMapper;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Services.GameWorld.Profiles;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class AgilityMappingTests
{
    private static IMapper CreateMapper() =>
        new MapperConfiguration(expression => expression.AddProfile<SkillsProfile>(), LoggerFactory.Create(_ => { })).CreateMapper();

    [TestMethod]
    public void Obstacle_definition_maps_object_id_experience_and_required_level()
    {
        var dto = CreateMapper().Map<AgilityObstacleDto>(new SkillsAgilityObstacleDefinition { ObjectId = 69506, Experience = 25m, RequiredLevel = 85 });

        Assert.AreEqual(69506, dto.ObjectId);
        Assert.AreEqual(25d, dto.Experience);
        Assert.AreEqual(85, dto.RequiredLevel);
    }

    [TestMethod]
    [DataRow((byte)1, AgilityCourseId.GnomeBasic, 39d)]
    [DataRow((byte)2, AgilityCourseId.GnomeAdvanced, 605d)]
    public void Course_definition_maps_persisted_id_to_course_identity(byte id, AgilityCourseId expected, double experience)
    {
        var dto = CreateMapper().Map<AgilityCourseDto>(new SkillsAgilityCourseDefinition { Id = id, CompletionExperience = (decimal)experience });

        Assert.AreEqual(expected, dto.Id);
        Assert.AreEqual(experience, dto.CompletionExperience);
    }
}
