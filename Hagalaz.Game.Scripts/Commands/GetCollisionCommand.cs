using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Commands
{
    public class GetCollisionCommand : IGameCommand
    {
        public string Name => "getcollision";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            var regionManager = args.Character.ServiceProvider.GetRequiredService<IMapRegionService>();
            var mapRegion = regionManager.FindMapRegion(args.Character.Location.RegionId, args.Character.Location.Dimension);
            var collision = mapRegion?.GetCollision(
                args.Character.Location.X - args.Character.Location.RegionX * 64,
                args.Character.Location.Y - args.Character.Location.RegionY * 64,
                args.Character.Location.Z) ?? CollisionFlag.FloorBlock;
            args.Character.SendChatMessage("Collision:" + collision,
                ChatMessageType.ConsoleText);
            return Task.CompletedTask;
        }
    }
}
