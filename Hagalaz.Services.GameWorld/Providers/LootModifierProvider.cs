using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Logic.Loot;

namespace Hagalaz.Services.GameWorld.Providers
{
    public class LootModifierProvider : ILootModifierProvider
    {
        private readonly List<IRandomObjectModifier> _modifiers;

        public LootModifierProvider(IRatesService ratesService)
        {
            _modifiers = [new CurrencyModifier(ratesService)];
        }

        public IEnumerable<IRandomObjectModifier> FindLootModifiers() => _modifiers;
    }
}