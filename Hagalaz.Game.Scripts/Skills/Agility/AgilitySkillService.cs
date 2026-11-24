using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Agility
{
    /// <summary>
    ///     Functionality for the Agility skill.
    /// </summary>
    public class AgilitySkillService : IAgilitySkillService
    {
        private readonly IAgilityService _agilityService;
        private readonly IReadOnlyList<IAgilityCourse> _courses;

        public AgilitySkillService(IAgilityService agilityService, IEnumerable<IAgilityCourse> courses)
        {
            _agilityService = agilityService;
            _courses = courses.ToArray();
        }

        /// <inheritdoc />
        public int GetRequiredLevel(int objectId) => _agilityService.FindObstacleByObjectId(objectId)?.RequiredLevel ?? 1;

        /// <inheritdoc />
        public void AddObstacleExperience(ICharacter character, int objectId)
        {
            var obstacle = _agilityService.FindObstacleByObjectId(objectId);
            if (obstacle != null)
            {
                character.Statistics.AddExperience(StatisticsConstants.Agility, obstacle.Experience);
            }
        }

        /// <inheritdoc />
        public void CheckCourseCompletion(ICharacter character)
        {
            var course = _courses.FirstOrDefault(c => c.IsCompleted(character));
            if (course == null)
            {
                return;
            }

            course.ClearProgress(character);
            var definition = _agilityService.FindCourseById(course.Id);
            if (definition != null)
            {
                character.Statistics.AddExperience(StatisticsConstants.Agility, definition.CompletionExperience);
            }
        }

        /// <inheritdoc />
        public void MakeNearbyNpcSpeak(ICharacter character, string npcSpeakText)
        {
            var npc = character.Viewport.VisibleNpcs.Where(n => n.SpeakingText == null && n.WithinRange(n, 5)).FirstOrDefault();
            if (npc != null)
            {
                npc.Speak(npcSpeakText);
            }
        }
    }
}
