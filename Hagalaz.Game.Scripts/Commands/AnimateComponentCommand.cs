using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class AnimateComponentCommand : IGameCommand
    {
        public string Name => "animatecomponent";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 3
                && short.TryParse(args.Arguments[0], out var id)
                && short.TryParse(args.Arguments[1], out var childId)
                && short.TryParse(args.Arguments[2], out var animId))
            {
                var inter = args.Character.Widgets.GetOpenWidget(id);
                inter?.SetAnimation(childId, animId);
            }
            return Task.CompletedTask;
        }
    }
}
