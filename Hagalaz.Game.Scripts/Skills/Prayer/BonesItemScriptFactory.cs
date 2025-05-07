using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Prayer
{
    public class BonesItemScriptFactory : IItemScriptFactory
    {
        private readonly IPrayerService _prayerService;

        public BonesItemScriptFactory(IPrayerService prayerService) => _prayerService = prayerService;

        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts()
        {
            var boneType = typeof(StandardBones);
            foreach (var bone in await _prayerService.FindAllByType(PrayerDtoType.Bones))
            {
                yield return (bone.ItemId, boneType);
            }
        }
    }
}
