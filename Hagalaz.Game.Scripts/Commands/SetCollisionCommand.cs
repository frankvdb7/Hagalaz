using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SetCollisionCommand : IGameCommand
    {
        public string Name => "setcollision";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            var regionManager = args.Character.ServiceProvider.GetRequiredService<IMapRegionService>();
            regionManager.FlagCollision(args.Character.Location, CollisionFlag.ObjectTile);
            regionManager.FlagCollision(args.Character.Location, CollisionFlag.ObjectBlock);
            regionManager.FlagCollision(args.Character.Location, CollisionFlag.ObjectAllowRange);
            return Task.CompletedTask;
        }
    }
}
