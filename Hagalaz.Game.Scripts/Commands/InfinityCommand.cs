using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Scripts.Commands
{
    public class InfinityCommand : IGameCommand
    {
        public string Name => "infinity";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            args.Character.QueueTask(new RsTickTask(args.Character.Statistics.Normalise));
            return Task.CompletedTask;
        }
    }
}
