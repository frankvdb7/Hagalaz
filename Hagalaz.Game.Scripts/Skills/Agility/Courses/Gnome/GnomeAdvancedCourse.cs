using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Agility.Courses.Gnome
{
    /// <summary>
    ///     The advanced Gnome Stronghold course.
    /// </summary>
    public class GnomeAdvancedCourse : IAgilityCourse
    {
        public AgilityCourseId Id => AgilityCourseId.GnomeAdvanced;

        public bool IsCompleted(ICharacter character) =>
            character.HasState<GnomeCourseLogBalanceState>() && character.HasState<GnomeCourseFirstObstacleNetState>()
                                                              && character.HasState<GnomeCourseFirstTreeBranchState>() && character.HasState<GnomeCourseTreeState>()
                                                              && character.HasState<GnomeCourseSignpostState>() && character.HasState<GnomeCoursePoleState>()
                                                              && character.HasState<GnomeCourseBarrierState>();

        public void ClearProgress(ICharacter character)
        {
            character.RemoveState<GnomeCourseLogBalanceState>();
            character.RemoveState<GnomeCourseFirstObstacleNetState>();
            character.RemoveState<GnomeCourseFirstTreeBranchState>();
            character.RemoveState<GnomeCourseTreeState>();
            character.RemoveState<GnomeCourseSignpostState>();
            character.RemoveState<GnomeCoursePoleState>();
            character.RemoveState<GnomeCourseBarrierState>();
        }
    }
}
