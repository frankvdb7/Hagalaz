using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class StringConfigCommand : IGameCommand
    {
        public string Name => "stringconfig";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 1 && short.TryParse(args.Arguments[0], out var id))
            {
                var value = args.Arguments.Length >= 2 ? args.Arguments[1] : string.Empty;
                args.Character.Configurations.SendGlobalCs2String(id, value);
            }
            return Task.CompletedTask;
        }
    }
}
