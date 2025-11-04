using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class DrawCharacterCommand : IGameCommand
    {
        public string Name => "drawcharacter";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 2
                && short.TryParse(args.Arguments[0], out var id)
                && short.TryParse(args.Arguments[1], out var childId))
            {
                var inter = args.Character.Widgets.GetOpenWidget(id);
                inter?.DrawCharacterHead(childId);
            }
            return Task.CompletedTask;
        }
    }
}
