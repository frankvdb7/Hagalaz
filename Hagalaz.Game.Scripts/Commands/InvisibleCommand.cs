using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Glow;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class InvisibleCommand : IGameCommand
    {
        private readonly IGlowBuilder _glowBuilder;
        public string Name { get; } = "invisible";
        public Permission Permission { get; } = Permission.GameModerator;

        public InvisibleCommand(IGlowBuilder glowBuilder) => _glowBuilder = glowBuilder;

        public Task Execute(GameCommandArgs args)
        {
            var character = args.Character;
            character.Appearance.Visible = !character.Appearance.Visible;
            if (!character.Appearance.Visible)
            {
                character.SendChatMessage("You are now INVISIBLE to all normal players.");
                if (character.Permissions.HasFlag(Permission.GameModerator))
                {
                    character.QueueGlow(_glowBuilder.Create().WithRed(0).WithGreen(0).WithBlue(0).WithAlpha(255).Build());
                }
            }
            else
            {
                character.SendChatMessage("You are now VISIBLE to all normal players.");
                if (character.Permissions.HasFlag(Permission.GameModerator))
                {
                    character.QueueGlow(_glowBuilder.Create().WithAlpha(0).Build());
                }
            }

            character.SendChatMessage("Type this command again to make yourself visible or invisible.");
            return Task.CompletedTask;
        }
    }
}