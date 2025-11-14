using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class ProjCommand : IGameCommand
    {
        public string Name => "proj";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length > 0 && int.TryParse(args.Arguments[0], out var gfxId))
            {
                var target = args.Character.Viewport.VisibleCreatures.FirstOrDefault(c => c != args.Character);
                if (target != null)
                {
                    var projectileBuilder = args.Character.ServiceProvider.GetRequiredService<IProjectileBuilder>();
                    projectileBuilder.Create().WithGraphicId(gfxId)
                        .FromCreature(args.Character)
                        .ToCreature(target)
                        .WithDuration(30)
                        .WithFromHeight(11)
                        .WithToHeight(11)
                        .WithSlope(20)
                        .WithDelay(30)
                        .Send();
                }
            }
            return Task.CompletedTask;
        }
    }
}
