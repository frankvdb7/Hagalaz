using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SystemUpdateCommand : IGameCommand
    {
        private readonly ISystemUpdateService _scheduler;
        public string Name { get; } = "update";
        public Permission Permission { get; } = Permission.SystemAdministrator;

        public SystemUpdateCommand(ISystemUpdateService scheduler) => _scheduler = scheduler;

        public Task Execute(GameCommandArgs args)
        {
            args.Handled = true;
            var ticks = Convert.ToInt16(args.Arguments[0]);
            _scheduler.ScheduleUpdate(ticks);
            return Task.CompletedTask;
        }
    }
}