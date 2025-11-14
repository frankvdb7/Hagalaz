using System.Linq;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Skills.Agility
{
    /// <summary>
    /// </summary>
    public static class Agility
    {
        /// <summary>
        ///     Checks the gnome course completion.
        /// </summary>
        /// <param name="character">The character.</param>
        public static void CheckGnomeCourseCompletion(ICharacter character)
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
                character.Statistics.AddExperience(StatisticsConstants.Agility, 39);
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
                character.Statistics.AddExperience(StatisticsConstants.Agility, 605);
            }
        }

        /// <summary>
        ///     Starts the gnome agility course.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="npcSpeakText">The NPC speak text.</param>
        public static void CheckGnomeCourseNpCs(ICharacter character, string npcSpeakText)
        {
            var npc = character.Viewport.VisibleCreatures.OfType<INpc>().Where(n => n.SpeakingText == null && n.WithinRange(n, 5)).FirstOrDefault();
            if (npc != null)
            {
                npc.Speak(npcSpeakText);
            }
        }
    }
}