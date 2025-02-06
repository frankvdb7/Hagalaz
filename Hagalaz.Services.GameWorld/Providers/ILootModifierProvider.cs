using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Logic.Random;

namespace Hagalaz.Services.GameWorld.Providers
{
    public interface ILootModifierProvider
    {
        IEnumerable<IRandomObjectModifier> FindLootModifiers();
    }
}