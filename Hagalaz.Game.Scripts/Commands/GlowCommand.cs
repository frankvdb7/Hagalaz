using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Glow;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class GlowCommand : IGameCommand
    {
        private readonly IGlowBuilder _glowBuilder;
        public string Name { get; } = "glow";
        public Permission Permission { get; } = Permission.SystemAdministrator;

        public GlowCommand(IGlowBuilder glowBuilder) => _glowBuilder = glowBuilder;

        public Task Execute(GameCommandArgs args)
        {
            var cmd = args.Arguments;
            var red = byte.Parse(cmd[1]);
            var green = byte.Parse(cmd[2]);
            var blue = byte.Parse(cmd[3]);
            var alpha = byte.Parse(cmd[4]);
            var duration = short.Parse(cmd[5]);
            var glow = _glowBuilder.Create().WithRed(red).WithGreen(green).WithBlue(blue).WithAlpha(alpha).WithDuration(duration).Build();
            args.Character.QueueGlow(glow);
            return Task.CompletedTask;
        }
    }
}