using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Region;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class GfxCommand : IGameCommand
    {
        private readonly IRegionUpdateBuilder _regionUpdateBuilder;
        public string Name { get; } = "gfx";
        public Permission Permission { get; } = Permission.SystemAdministrator;

        public GfxCommand(IRegionUpdateBuilder regionUpdateBuilder)
        {
            _regionUpdateBuilder = regionUpdateBuilder;
        }

        public Task Execute(GameCommandArgs args)
        {
            var cmd = args.Arguments;
            var gfxID = int.Parse(cmd[1]);
            var height = 0;
            var rotation = 0;
            if (cmd.Length > 2)
                height = int.Parse(cmd[2]);
            if (cmd.Length > 3)
                rotation = int.Parse(cmd[3]);
            var gfx = Graphic.Create(gfxID, 0, height, rotation);
            var update = _regionUpdateBuilder.Create().WithLocation(args.Character.Location).WithGraphic(gfx).Build();
            args.Character.Region.QueueUpdate(update);
            return Task.CompletedTask;
        }
    }
}