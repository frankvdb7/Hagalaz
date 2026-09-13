using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SearchNpcCommand : IGameCommand
    {
        private readonly INpcService _service;

        public string Name { get; } = "searchnpc";

        public Permission Permission { get; } = Permission.GameAdministrator;

        public SearchNpcCommand(INpcService service) => _service = service;

        public async Task Execute(GameCommandArgs args)
        {
            args.Handled = true;
            var name = string.Join(" ", args.Arguments).ToLower();

            var results = await Task.Run(() =>
            {
                var matches = new List<string>();
                for (var i = 0; i < _service.GetNpcDefinitionCount(); i++)
                {
                    var def = _service.FindNpcDefinitionById(i);
                    if (def != null && def.Name.Contains(name, System.StringComparison.CurrentCultureIgnoreCase))
                    {
                        matches.Add($"[{i}]: {def.Name}");
                    }
                }

                return matches;
            });

            args.Character.QueueTask(new RsTask(() =>
            {
                foreach (var result in results)
                {
                    args.Character.SendChatMessage(result, ChatMessageType.ConsoleText);
                }
            }, 1));
        }
    }
}
