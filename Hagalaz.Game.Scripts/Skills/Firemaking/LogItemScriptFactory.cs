using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Skills.Firemaking
{
    public class LogItemScriptFactory : IItemScriptFactory
    {
        private readonly IFiremakingService _firemakingService;

        public LogItemScriptFactory(IFiremakingService firemakingService)
        {
            _firemakingService = firemakingService;
        }

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            var logType = typeof(StandardLog);
            foreach (var log in await _firemakingService.FindAllLogs())
            {
                yield return (log.ItemId, logType);
            }
        }
    }
}