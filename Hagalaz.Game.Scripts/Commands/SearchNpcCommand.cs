using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SearchNpcCommand : IGameCommand
    {
        private readonly INpcService _service;

        public string Name { get; } = "searchnpc";

        public Permission Permission { get; } = Permission.GameAdministrator;

        public SearchNpcCommand(INpcService service) => _service = service;

        public Task Execute(GameCommandArgs args)
        {
            args.Handled = true;
            var name = string.Join(" ", args.Arguments).ToLower();
            Task.Run(async () =>
            {
                await Task.CompletedTask;
                for (var i = 0; i < _service.GetNpcDefinitionCount(); i++)
                {
                    var def = _service.FindNpcDefinitionById(i);
                    if (def != null && def.Name.Contains(name, System.StringComparison.CurrentCultureIgnoreCase))
                    {
                        args.Character.SendChatMessage($"[{i}]: {def}", ChatMessageType.ConsoleText);
                    }
                }
            });
            return Task.CompletedTask;
        }
    }
}