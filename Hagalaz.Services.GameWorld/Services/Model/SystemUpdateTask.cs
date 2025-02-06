using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Messages.Protocol;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services.Model
{
    public class SystemUpdateTask : ISystemUpdateTask
    {
        private readonly ILogger<SystemUpdateTask> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ICharacterStore _characterStore;

        public SystemUpdateTask(ILogger<SystemUpdateTask> logger, IHostApplicationLifetime applicationLifetime, ICharacterStore characterStore)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _characterStore = characterStore;
        }

        public async Task ExecuteAsync(DateTimeOffset executionTime, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(executionTime, DateTimeOffset.Now);

            try
            {
                var delayTimeSpan = executionTime - DateTime.Now;
                await foreach (var character in _characterStore.FindAllAsync().WithCancellation(cancellationToken))
                {
                    character.Session.SendMessage(new SetSystemUpdateTickMessage
                    {
                        Tick = delayTimeSpan.Milliseconds / 600
                    });
                }

                await Task.Delay(delayTimeSpan, cancellationToken);

                _applicationLifetime.StopApplication();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing system update task");
            }
        }
    }
}