using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Common;

namespace Hagalaz.Game.Scripts.Commands
{
    public class ForceMovementCommand : IGameCommand
    {
        public string Name => "forcemovement";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            var builder = args.Character.ServiceProvider.GetRequiredService<IMovementBuilder>();
            var mov = builder.Create()
                .WithStart(args.Character.Location)
                .WithEnd(Game.Abstractions.Model.Location.Create(args.Character.Location.X + RandomStatic.Generator.Next(5),
                    args.Character.Location.Y + RandomStatic.Generator.Next(5),
                    args.Character.Location.Z,
                    args.Character.Location.Dimension))
                .WithEndSpeed(40)
                .Build();
            args.Character.Movement.Teleport(Teleport.Create(mov.EndLocation));
            args.Character.QueueForceMovement(mov);
            return Task.CompletedTask;
        }
    }
}
