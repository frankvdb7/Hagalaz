using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Teleport;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class TeleportCommand : IGameCommand
    {
        private readonly ITeleportBuilder _teleportBuilder;
        public string Name { get; } = "tele";
        public Permission Permission { get; } = Permission.GameModerator;

        public TeleportCommand(ITeleportBuilder teleportBuilder) => _teleportBuilder = teleportBuilder;

        public Task Execute(GameCommandArgs args)
        {
            var cmd = args.Arguments;
            var x = int.Parse(cmd[0]);
            var y = int.Parse(cmd[1]);
            var z = cmd.Length > 2 ? int.Parse(cmd[2]) : args.Character.Location.Z;
            var dim = cmd.Length > 3 ? int.Parse(cmd[3]) : args.Character.Location.Dimension;
            
            args.Character.Movement.Teleport(_teleportBuilder.Create().WithX(x).WithY(y).WithZ(z).WithDimension(dim).Build());
            return Task.CompletedTask;
        }
    }
}