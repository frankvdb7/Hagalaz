using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class ToPlayerCommand : IGameCommand
    {
        public string Name => "toplayer";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            args.Character.Appearance.TurnToPlayer();
            return Task.CompletedTask;
        }
    }
}
