using System;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Resources;

namespace Hagalaz.Services.GameWorld.Logic.Loot
{
    public class RingOfWealthModifier : IRandomObjectModifier
    {
        public void Apply(RandomObjectContext context)
        {
            if (context is not CharacterLootContext lootContext)
            {
                return;
            }

            if (!lootContext.Character.HasState(StateType.RingOfWealthEquiped))
            {
                return;
            }

            if (!(Random.Shared.NextDouble() <= 0.025))
            {
                return;
            }

            lootContext.ModifiedProbability *= 2;
            lootContext.Character.SendChatMessage(GameStrings.RingOfWealthEffect);
        }
    }
}