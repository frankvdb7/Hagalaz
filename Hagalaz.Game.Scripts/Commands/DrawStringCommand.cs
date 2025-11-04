using System.Text;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class DrawStringCommand : IGameCommand
    {
        public string Name => "drawstring";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 2
                && short.TryParse(args.Arguments[0], out var id)
                && short.TryParse(args.Arguments[1], out var childId))
            {
                var inter = args.Character.Widgets.GetOpenWidget(id);
                if (inter != null)
                {
                    var bld = new StringBuilder();
                    for (int i = 2; i < args.Arguments.Length; i++)
                    {
                        bld.Append(args.Arguments[i] + (i + 1 >= args.Arguments.Length ? "" : " "));
                    }
                    inter.DrawString(childId, bld.ToString());
                }
            }
            return Task.CompletedTask;
        }
    }
}
