using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class DrawModelCommand : IGameCommand
    {
        public string Name => "drawmodel";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 3
                && short.TryParse(args.Arguments[0], out var id)
                && short.TryParse(args.Arguments[1], out var childId)
                && int.TryParse(args.Arguments[2], out var modelId))
            {
                var inter = args.Character.Widgets.GetOpenWidget(id);
                inter?.DrawModel(childId, modelId);
            }
            return Task.CompletedTask;
        }
    }
}
