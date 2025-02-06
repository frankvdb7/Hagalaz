using System.Linq;
using Hagalaz.Game.Abstractions.Features.States;
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
            if (character.HasState(StateType.GnomeCourseLogBalance) && character.HasState(StateType.GnomeCourseFirstObstacleNet)
                                                                    && character.HasState(StateType.GnomeCourseFirstTreeBranch) && character.HasState(StateType.GnomeCourseBalancingRope)
                                                                    && character.HasState(StateType.GnomeCourseTreeBranchDown) && character.HasState(StateType.GnomeCourseSecondObstacleNet)
                                                                    && character.HasState(StateType.GnomeCourseObstaclePipe))
            {
                character.RemoveState(StateType.GnomeCourseLogBalance);
                character.RemoveState(StateType.GnomeCourseFirstObstacleNet);
                character.RemoveState(StateType.GnomeCourseFirstTreeBranch);
                character.RemoveState(StateType.GnomeCourseBalancingRope);
                character.RemoveState(StateType.GnomeCourseTreeBranchDown);
                character.RemoveState(StateType.GnomeCourseSecondObstacleNet);
                character.RemoveState(StateType.GnomeCourseObstaclePipe);
                character.Statistics.AddExperience(StatisticsConstants.Agility, 39);
            }
            // check advanced gnome course completion
            else if (character.HasState(StateType.GnomeCourseLogBalance) && character.HasState(StateType.GnomeCourseFirstObstacleNet)
                                                                         && character.HasState(StateType.GnomeCourseFirstTreeBranch) && character.HasState(StateType.GnomeCourseTree)
                                                                         && character.HasState(StateType.GnomeCourseSignpost) && character.HasState(StateType.GnomeCoursePole)
                                                                         && character.HasState(StateType.GnomeCourseBarrier))
            {
                character.RemoveState(StateType.GnomeCourseLogBalance);
                character.RemoveState(StateType.GnomeCourseFirstObstacleNet);
                character.RemoveState(StateType.GnomeCourseFirstTreeBranch);
                character.RemoveState(StateType.GnomeCourseTree);
                character.RemoveState(StateType.GnomeCourseSignpost);
                character.RemoveState(StateType.GnomeCoursePole);
                character.RemoveState(StateType.GnomeCourseBarrier);
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