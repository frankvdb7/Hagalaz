using System.Linq;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Agility
{
    /// <summary>
    /// </summary>
    public static class Agility
    {
        /// <summary>
        ///     Grants the persisted experience of the obstacle with the given object id. Nothing is granted when the obstacle has no definition.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="agilityService">The agility service.</param>
        /// <param name="objectId">The object id of the obstacle.</param>
        public static void AddObstacleExperience(ICharacter character, IAgilityService agilityService, int objectId)
        {
            var obstacle = agilityService.FindObstacleByObjectId(objectId).Result;
            if (obstacle != null)
            {
                character.Statistics.AddExperience(StatisticsConstants.Agility, obstacle.Experience);
            }
        }

        private static void AddCourseCompletionExperience(ICharacter character, IAgilityService agilityService, AgilityCourseId courseId)
        {
            var course = agilityService.FindCourseById(courseId).Result;
            if (course != null)
            {
                character.Statistics.AddExperience(StatisticsConstants.Agility, course.CompletionExperience);
            }
        }

        /// <summary>
        ///     Checks the gnome course completion.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="agilityService">The agility service.</param>
        public static void CheckGnomeCourseCompletion(ICharacter character, IAgilityService agilityService)
        {
            // check basic gnome course completion
            if (character.HasState<GnomeCourseLogBalanceState>() && character.HasState<GnomeCourseFirstObstacleNetState>()
                                                                    && character.HasState<GnomeCourseFirstTreeBranchState>() && character.HasState<GnomeCourseBalancingRopeState>()
                                                                    && character.HasState<GnomeCourseTreeBranchDownState>() && character.HasState<GnomeCourseSecondObstacleNetState>()
                                                                    && character.HasState<GnomeCourseObstaclePipeState>())
            {
                character.RemoveState<GnomeCourseLogBalanceState>();
                character.RemoveState<GnomeCourseFirstObstacleNetState>();
                character.RemoveState<GnomeCourseFirstTreeBranchState>();
                character.RemoveState<GnomeCourseBalancingRopeState>();
                character.RemoveState<GnomeCourseTreeBranchDownState>();
                character.RemoveState<GnomeCourseSecondObstacleNetState>();
                character.RemoveState<GnomeCourseObstaclePipeState>();
                AddCourseCompletionExperience(character, agilityService, AgilityCourseId.GnomeBasic);
            }
            // check advanced gnome course completion
            else if (character.HasState<GnomeCourseLogBalanceState>() && character.HasState<GnomeCourseFirstObstacleNetState>()
                                                                         && character.HasState<GnomeCourseFirstTreeBranchState>() && character.HasState<GnomeCourseTreeState>()
                                                                         && character.HasState<GnomeCourseSignpostState>() && character.HasState<GnomeCoursePoleState>()
                                                                         && character.HasState<GnomeCourseBarrierState>())
            {
                character.RemoveState<GnomeCourseLogBalanceState>();
                character.RemoveState<GnomeCourseFirstObstacleNetState>();
                character.RemoveState<GnomeCourseFirstTreeBranchState>();
                character.RemoveState<GnomeCourseTreeState>();
                character.RemoveState<GnomeCourseSignpostState>();
                character.RemoveState<GnomeCoursePoleState>();
                character.RemoveState<GnomeCourseBarrierState>();
                AddCourseCompletionExperience(character, agilityService, AgilityCourseId.GnomeAdvanced);
            }
        }

        /// <summary>
        ///     Starts the gnome agility course.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="npcSpeakText">The NPC speak text.</param>
        public static void CheckGnomeCourseNpCs(ICharacter character, string npcSpeakText)
        {
            var npc = character.Viewport.VisibleNpcs.Where(n => n.SpeakingText == null && n.WithinRange(n, 5)).FirstOrDefault();
            if (npc != null)
            {
                npc.Speak(npcSpeakText);
            }
        }
    }
}