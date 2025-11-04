using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class BitConfigCommand : IGameCommand
    {
        public string Name => "bitconfig";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 2
                && int.TryParse(args.Arguments[0], out var id)
                && int.TryParse(args.Arguments[1], out var value))
            {
                args.Character.Configurations.SendBitConfiguration((short)id, value);
            }
            return Task.CompletedTask;
        }
    }
}
