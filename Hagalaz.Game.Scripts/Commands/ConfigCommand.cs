using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class ConfigCommand : IGameCommand
    {
        public string Name => "config";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 2
                && int.TryParse(args.Arguments[0], out var id)
                && int.TryParse(args.Arguments[1], out var value))
            {
                args.Character.Configurations.SendStandardConfiguration((short)id, value);
            }
            return Task.CompletedTask;
        }
    }
}
