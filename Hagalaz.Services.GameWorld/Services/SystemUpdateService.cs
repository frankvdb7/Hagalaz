using System;
using System.Threading;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class SystemUpdateService : ISystemUpdateService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<GameServerOptions> _options;
        private ISystemUpdateTask? _activeTask;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;

        /// <summary>
        /// 
        /// </summary>
        public bool SystemUpdateScheduled => _activeTask != null;

        /// <summary>
        /// 
        /// </summary>
        public SystemUpdateService(IServiceProvider serviceProvider, IOptions<GameServerOptions> options)
        {
            _serviceProvider = serviceProvider;
            _options = options;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tickTime"></param>
        public void ScheduleUpdate(int tickTime) => ScheduleUpdate(DateTime.UtcNow.Add(tickTime * _options.Value.TickTimeSpan));

        /// <summary>
        /// 
        /// </summary>
        public void CancelUpdate() => _cancellationTokenSource.Cancel();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="executionTime"></param>
        public void ScheduleUpdate(DateTimeOffset executionTime)
        {
            CancelUpdate();

            _activeTask = _serviceProvider.GetRequiredService<ISystemUpdateTask>();
            _activeTask.ExecuteAsync(executionTime, _cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _cancellationTokenSource.Dispose();
        }
    }
}