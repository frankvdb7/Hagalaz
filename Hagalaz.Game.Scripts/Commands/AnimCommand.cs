using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class AnimCommand : IGameCommand
    {
        private readonly IAnimationBuilder _animationBuilder;
        public string Name { get; } = "anim";
        public Permission Permission { get; } = Permission.SystemAdministrator;

        public AnimCommand(IAnimationBuilder animationBuilder) => _animationBuilder = animationBuilder;

        public Task Execute(GameCommandArgs args)
        {
            var cmd = args.Arguments;
            var id = int.Parse(cmd[1]);
            var delay = 0;
            if (cmd.Length > 2)
                delay = int.Parse(cmd[2]);
            var anim = _animationBuilder.Create().WithId(id).WithDelay(delay).Build();
            args.Character.QueueAnimation(anim);
            return Task.CompletedTask;
        }
    }
}