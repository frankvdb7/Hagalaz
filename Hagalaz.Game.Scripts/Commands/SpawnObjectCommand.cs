using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SpawnObjectCommand : IGameCommand
    {
        public string Name => "spawnobject";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 3
                && int.TryParse(args.Arguments[0], out var id)
                && int.TryParse(args.Arguments[1], out var shape)
                && int.TryParse(args.Arguments[2], out var rotation))
            {
                var goBuilder = args.Character.ServiceProvider.GetRequiredService<IGameObjectBuilder>();
                var gameObject = goBuilder.Create()
                    .WithId(id)
                    .WithLocation(args.Character.Location)
                    .WithShape((ShapeType)shape)
                    .WithRotation(rotation)
                    .Build();
                args.Character.Region.Add(gameObject);
            }
            return Task.CompletedTask;
        }
    }
}
