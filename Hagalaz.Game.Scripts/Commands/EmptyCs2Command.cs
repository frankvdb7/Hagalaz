using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class EmptyCs2Command : IGameCommand
    {
        public string Name => "emptycs2";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 1 && int.TryParse(args.Arguments[0], out var scriptId))
            {
                args.Character.Configurations.SendCs2Script(scriptId, []);
            }
            return Task.CompletedTask;
        }
    }
}
