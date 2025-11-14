using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Services.GameWorld.Logic.Loot
{
    public class RingOfWealthModifier : IRandomObjectModifier
    {
        private readonly IRandomProvider _randomProvider;

        public RingOfWealthModifier(IRandomProvider randomProvider)
        {
            _randomProvider = randomProvider;
        }

        public void Apply(RandomObjectContext context)
        {
            if (context is not CharacterLootContext lootContext)
            {
                return;
            }

            if (!lootContext.Character.HasState<RingOfWealthEquipedState>())
            {
                return;
            }

            if (!(_randomProvider.NextDouble() <= 0.025))
            {
                return;
            }

            lootContext.ModifiedProbability *= 2;
            lootContext.Character.SendChatMessage(GameStrings.RingOfWealthEffect);
        }
    }
}