using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class ToggleComponentCommand : IGameCommand
    {
        public string Name => "togglecomponent";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 3
                && short.TryParse(args.Arguments[0], out var id)
                && short.TryParse(args.Arguments[1], out var childId)
                && bool.TryParse(args.Arguments[2], out var visible))
            {
                var inter = args.Character.Widgets.GetOpenWidget(id);
                inter?.SetVisible(childId, visible);
            }
            return Task.CompletedTask;
        }
    }
}
