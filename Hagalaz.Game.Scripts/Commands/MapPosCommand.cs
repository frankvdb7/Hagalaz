using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class MapPosCommand : IGameCommand
    {
        public string Name => "mappos";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 2 && int.TryParse(args.Arguments[0], out var absX) && int.TryParse(args.Arguments[1], out var absY))
            {
                var mapX = -1;
                var mapY = -1;
                var location = Game.Abstractions.Model.Location.Create(absX, absY, args.Character.Location.Z, args.Character.Location.Dimension);
                args.Character.Viewport.GetLocalPosition(location, ref mapX, ref mapY);
                args.Character.SendChatMessage($"X:{mapX} Y:{mapY}");
            }
            return Task.CompletedTask;
        }
    }
}
