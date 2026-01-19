using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Commands
{
    public class LevelCommand : IGameCommand
    {
        public string Name => "level";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 2
                && byte.TryParse(args.Arguments[0], out var skillId)
                && byte.TryParse(args.Arguments[1], out var level)
                && skillId >= 0
                && skillId <= 24)
            {
                args.Character.Statistics.SetSkillExperience(skillId, StatisticsHelpers.ExperienceForLevel(level));
            }
            return Task.CompletedTask;
        }
    }
}
