using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class Cs2ConfigCommand : IGameCommand
    {
        public string Name => "cs2config";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 2
                && int.TryParse(args.Arguments[0], out var id)
                && int.TryParse(args.Arguments[1], out var value))
            {
                args.Character.Configurations.SendGlobalCs2Int((short)id, value);
            }
            return Task.CompletedTask;
        }
    }
}
