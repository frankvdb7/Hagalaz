using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SetColorCommand : IGameCommand
    {
        public string Name => "setcolor";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length >= 2
                && int.TryParse(args.Arguments[0], out var part)
                && int.TryParse(args.Arguments[1], out var val))
            {
                try
                {
                    args.Character.Appearance.SetColor((ColorType)part, val);
                }
                catch (Exception ex)
                {
                    args.Character.SendChatMessage("Something went wrong: " + ex.Message);
                }
            }
            return Task.CompletedTask;
        }
    }
}
