using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SpawnNpcCommand : IGameCommand
    {
        public string Name => "spawnnpc";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            if (args.Arguments.Length > 0 && int.TryParse(args.Arguments[0], out var id))
            {
                var builder = args.Character.ServiceProvider.GetRequiredService<INpcBuilder>();
                try
                {
                    builder
                        .Create()
                        .WithId(id)
                        .WithLocation(args.Character.Location)
                        .Spawn();
                    args.Character.SendChatMessage("Done :)", ChatMessageType.ConsoleText);
                }
                catch (Exception)
                {
                    args.Character.SendChatMessage("Fail :(", ChatMessageType.ConsoleText);
                }
            }
            return Task.CompletedTask;
        }
    }
}
