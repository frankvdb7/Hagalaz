using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class DrawItemCommand : IGameCommand
    {
        public string Name => "drawitem";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 4
                && short.TryParse(args.Arguments[0], out var id)
                && short.TryParse(args.Arguments[1], out var childId)
                && short.TryParse(args.Arguments[2], out var itemId)
                && int.TryParse(args.Arguments[3], out var amount))
            {
                var inter = args.Character.Widgets.GetOpenWidget(id);
                inter?.DrawItem(childId, itemId, amount);
            }
            return Task.CompletedTask;
        }
    }
}
