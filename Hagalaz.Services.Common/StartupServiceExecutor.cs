using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.Common
{
    public class StartupServiceExecutor : BackgroundService
    {
        private readonly ILogger<StartupServiceExecutor> _logger;
        private readonly IServiceProvider _serviceProvider;

        public StartupServiceExecutor(ILogger<StartupServiceExecutor> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                List<Exception>? exceptions = null;
                var startupServices = _serviceProvider.GetServices<IStartupService>();
                foreach (var task in startupServices)
                {
                    try
                    {
                        await task.LoadAsync().WaitAsync(TimeSpan.FromSeconds(30), stoppingToken);
                        _logger.LogTrace("Startup task '{TaskType}' loaded", task.GetType().Name);
                    }
                    catch (Exception ex)
                    {
                        exceptions ??= [];

                        exceptions.Add(ex);
                    }
                }

                // Throw an aggregate exception if there were any exceptions
                if (exceptions != null)
                {
                    throw new AggregateException(exceptions);
                }
                _logger.LogDebug("Startup tasks successfully loaded");
            }
            catch (Exception globalEx)
            {
                _logger.LogError(globalEx, "An error occurred starting while executing start-up tasks");
            }
        }
    }
}
