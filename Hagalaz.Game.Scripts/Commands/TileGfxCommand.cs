using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Graphic;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class TileGfxCommand : IGameCommand
    {
        private readonly IGraphicBuilder _graphicBuilder;
        public string Name { get; } = "tilegfx";
        public Permission Permission { get; } = Permission.SystemAdministrator;

        public TileGfxCommand(IGraphicBuilder graphicBuilder) => _graphicBuilder = graphicBuilder;

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
            var gfx = _graphicBuilder.Create().WithId(gfxID).WithHeight(height).WithRotation(rotation).Build();
            args.Character.QueueGraphic(gfx);
            return Task.CompletedTask;
        }
    }
}