using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SetSkullCommand : IGameCommand
    {
        public string Name => "setskull";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length > 0 && short.TryParse(args.Arguments[0], out var icon))
            {
                args.Character.Appearance.SkullIcon = (SkullIcon)icon;
            }
            return Task.CompletedTask;
        }
    }
}
