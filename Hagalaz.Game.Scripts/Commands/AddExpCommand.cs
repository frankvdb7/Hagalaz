using System;
using System.Globalization;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class AddExpCommand : IGameCommand
    {
        public string Name => "addexp";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 2
                && byte.TryParse(args.Arguments[0], out var skillId)
                && double.TryParse(args.Arguments[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var exp)
                && skillId >= 0
                && skillId <= 24)
            {
                try
                {
                    args.Character.Statistics.AddExperience(skillId, exp);
                }
                catch (Exception ex)
                {
                    args.Character.SendChatMessage($"Something went wrong: {ex.Message}");
                }
            }
            else
            {
                args.Character.SendChatMessage("Invalid syntax. Use: addexp [skillId] [experience]");
            }

            return Task.CompletedTask;
        }
    }
}
