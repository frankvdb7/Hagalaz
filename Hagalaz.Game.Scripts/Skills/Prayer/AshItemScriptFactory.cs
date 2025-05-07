using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Prayer
{
    public class AshItemScriptFactory : IItemScriptFactory
    {
        private readonly IPrayerService _prayerService;

        public AshItemScriptFactory(IPrayerService prayerService) => _prayerService = prayerService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            var ashType = typeof(StandardAsh);
            foreach (var ash in await _prayerService.FindAllByType(PrayerDtoType.Ashes))
            {
                yield return (ash.ItemId, ashType);
            }
        }
    }
}
