using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using NSubstitute;
using AgilityScripts = Hagalaz.Game.Scripts.Skills.Agility.Agility;

namespace Hagalaz.Game.Scripts.Tests.Skills.Agility;

[TestClass]
public sealed class AgilityTests
{
    private IAgilityService _agilityService = null!;
    private ICharacter _character = null!;
    private ICharacterStatistics _statistics = null!;

    [TestInitialize]
    public void Initialize()
    {
        _agilityService = Substitute.For<IAgilityService>();
        _agilityService.FindCourseById(AgilityCourseId.GnomeBasic)
            .Returns(new AgilityCourseDto { Id = AgilityCourseId.GnomeBasic, CompletionExperience = 39 });
        _agilityService.FindCourseById(AgilityCourseId.GnomeAdvanced)
            .Returns(new AgilityCourseDto { Id = AgilityCourseId.GnomeAdvanced, CompletionExperience = 605 });
        _statistics = Substitute.For<ICharacterStatistics>();
        _character = Substitute.For<ICharacter>();
        _character.Statistics.Returns(_statistics);
    }

    [TestMethod]
    public void CheckGnomeCourseCompletion_BasicCourse_GrantsPersistedExperienceAndClearsStates()
    {
        GiveBasicCourseStates();

        AgilityScripts.CheckGnomeCourseCompletion(_character, _agilityService);

        _statistics.Received(1).AddExperience(StatisticsConstants.Agility, 39);
        _statistics.Received(1).AddExperience(Arg.Any<int>(), Arg.Any<double>());
        _character.Received(1).RemoveState<GnomeCourseObstaclePipeState>();
        _character.DidNotReceive().RemoveState<GnomeCourseBarrierState>();
    }

    [TestMethod]
    public void CheckGnomeCourseCompletion_AdvancedCourse_GrantsPersistedExperienceAndClearsStates()
    {
        GiveAdvancedCourseStates();

        AgilityScripts.CheckGnomeCourseCompletion(_character, _agilityService);

        _statistics.Received(1).AddExperience(StatisticsConstants.Agility, 605);
        _statistics.Received(1).AddExperience(Arg.Any<int>(), Arg.Any<double>());
        _character.Received(1).RemoveState<GnomeCourseBarrierState>();
        _character.DidNotReceive().RemoveState<GnomeCourseObstaclePipeState>();
    }

    [TestMethod]
    public void CheckGnomeCourseCompletion_IncompleteCourse_GrantsNothing()
    {
        _character.HasState<GnomeCourseLogBalanceState>().Returns(true);

        AgilityScripts.CheckGnomeCourseCompletion(_character, _agilityService);

        _statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void CheckGnomeCourseCompletion_MissingCourseDefinition_ClearsStatesButGrantsNothing()
    {
        _agilityService.FindCourseById(AgilityCourseId.GnomeBasic).Returns((AgilityCourseDto?)null);
        GiveBasicCourseStates();

        AgilityScripts.CheckGnomeCourseCompletion(_character, _agilityService);

        _statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
        _character.Received(1).RemoveState<GnomeCourseObstaclePipeState>();
    }

    [TestMethod]
    public void AddObstacleExperience_KnownObstacle_GrantsPersistedExperience()
    {
        _agilityService.FindObstacleByObjectId(69526)
            .Returns(new AgilityObstacleDto { ObjectId = 69526, Experience = 7.5, RequiredLevel = 1 });

        AgilityScripts.AddObstacleExperience(_character, _agilityService, 69526);

        _statistics.Received(1).AddExperience(StatisticsConstants.Agility, 7.5);
    }

    [TestMethod]
    public void AddObstacleExperience_UnknownObstacle_GrantsNothing()
    {
        _agilityService.FindObstacleByObjectId(1).Returns((AgilityObstacleDto?)null);

        AgilityScripts.AddObstacleExperience(_character, _agilityService, 1);

        _statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    private void GiveBasicCourseStates()
    {
        _character.HasState<GnomeCourseLogBalanceState>().Returns(true);
        _character.HasState<GnomeCourseFirstObstacleNetState>().Returns(true);
        _character.HasState<GnomeCourseFirstTreeBranchState>().Returns(true);
        _character.HasState<GnomeCourseBalancingRopeState>().Returns(true);
        _character.HasState<GnomeCourseTreeBranchDownState>().Returns(true);
        _character.HasState<GnomeCourseSecondObstacleNetState>().Returns(true);
        _character.HasState<GnomeCourseObstaclePipeState>().Returns(true);
    }

    private void GiveAdvancedCourseStates()
    {
        _character.HasState<GnomeCourseLogBalanceState>().Returns(true);
        _character.HasState<GnomeCourseFirstObstacleNetState>().Returns(true);
        _character.HasState<GnomeCourseFirstTreeBranchState>().Returns(true);
        _character.HasState<GnomeCourseTreeState>().Returns(true);
        _character.HasState<GnomeCourseSignpostState>().Returns(true);
        _character.HasState<GnomeCoursePoleState>().Returns(true);
        _character.HasState<GnomeCourseBarrierState>().Returns(true);
    }
}
