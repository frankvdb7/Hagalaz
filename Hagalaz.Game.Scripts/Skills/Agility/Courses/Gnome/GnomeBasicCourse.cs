using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Agility.Courses.Gnome
{
    /// <summary>
    ///     The basic Gnome Stronghold course.
    /// </summary>
    public class GnomeBasicCourse : IAgilityCourse
    {
        public AgilityCourseId Id => AgilityCourseId.GnomeBasic;

        public bool IsCompleted(ICharacter character) =>
            character.HasState<GnomeCourseLogBalanceState>() && character.HasState<GnomeCourseFirstObstacleNetState>()
                                                              && character.HasState<GnomeCourseFirstTreeBranchState>() && character.HasState<GnomeCourseBalancingRopeState>()
                                                              && character.HasState<GnomeCourseTreeBranchDownState>() && character.HasState<GnomeCourseSecondObstacleNetState>()
                                                              && character.HasState<GnomeCourseObstaclePipeState>();

        public void ClearProgress(ICharacter character)
        {
            character.RemoveState<GnomeCourseLogBalanceState>();
            character.RemoveState<GnomeCourseFirstObstacleNetState>();
            character.RemoveState<GnomeCourseFirstTreeBranchState>();
            character.RemoveState<GnomeCourseBalancingRopeState>();
            character.RemoveState<GnomeCourseTreeBranchDownState>();
            character.RemoveState<GnomeCourseSecondObstacleNetState>();
            character.RemoveState<GnomeCourseObstaclePipeState>();
        }
    }
}
