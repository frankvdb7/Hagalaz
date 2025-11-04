using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class ToNpcCommand : IGameCommand
    {
        public string Name => "tonpc";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 1 && short.TryParse(args.Arguments[0], out var id))
            {
                args.Character.Appearance.TurnToNpc(id);
            }
            return Task.CompletedTask;
        }
    }
}
