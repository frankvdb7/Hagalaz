using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class RefillCommand : IGameCommand
    {
        public string Name => "refill";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            args.Character.Statistics.Normalise();
            return Task.CompletedTask;
        }
    }
}
