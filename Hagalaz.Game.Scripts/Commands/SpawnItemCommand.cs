using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SpawnItemCommand : IGameCommand
    {
        public string Name => "spawnitem";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 3
                && int.TryParse(args.Arguments[0], out var itemId)
                && int.TryParse(args.Arguments[1], out var itemAmount)
                && int.TryParse(args.Arguments[2], out var amountOfTicks))
            {
                var groundItemBuilder = args.Character.ServiceProvider.GetRequiredService<IGroundItemBuilder>();
                groundItemBuilder.Create()
                    .WithItem(builder => builder.Create().WithId(itemId).WithCount(itemAmount))
                    .WithLocation(args.Character.Location)
                    .WithTicks(amountOfTicks)
                    .Spawn();
            }
            return Task.CompletedTask;
        }
    }
}
