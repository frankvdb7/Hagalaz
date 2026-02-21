using System;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public class GlobalExceptionService : IGlobalExceptionService
    {
        public GlobalExceptionService(ILogger<GlobalExceptionService> logger)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                logger.LogCritical(eventArgs.ExceptionObject as Exception, "Error while running the application");
            };
        }
    }
}