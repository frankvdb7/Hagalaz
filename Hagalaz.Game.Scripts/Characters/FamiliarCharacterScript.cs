using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Characters
{
    public class FamiliarCharacterScript : CharacterScriptBase, IDefaultCharacterScript,
        IHydratable<HydratedFamiliarDto>, IHydratable<IReadOnlyList<HydratedItem>>
    {
        private readonly INpcBuilder _npcBuilder;
        private readonly IFamiliarScriptProvider _familiarScriptProvider;
        private readonly ISummoningService _summoningService;
        private HydratedFamiliarDto? _familiar;
        private IReadOnlyList<HydratedItem>? _inventory;

        public FamiliarCharacterScript(
            ICharacterContextAccessor contextAccessor,
            INpcBuilder npcBuilder,
            IFamiliarScriptProvider familiarScriptProvider,
            ISummoningService summoningService)
            : base(contextAccessor)
        {
            _npcBuilder = npcBuilder;
            _familiarScriptProvider = familiarScriptProvider;
            _summoningService = summoningService;
        }

        protected override void Initialize() { }

        public void Hydrate(HydratedFamiliarDto hydration) => _familiar = hydration;

        public void Hydrate(IReadOnlyList<HydratedItem> hydration) => _inventory = hydration;

        public override void OnRegistered()
        {
            var familiar = _familiar;
            var inventory = _inventory;
            if (familiar is null || familiar.FamiliarId == 0)
            {
                _familiar = null;
                _inventory = null;
                return;
            }

            try
            {
                var definition = _summoningService.FindDefinitionByNpcIdSync(familiar.FamiliarId);
                if (definition is null)
                {
                    return;
                }

                var scriptType = _familiarScriptProvider.FindFamiliarScriptTypeById(familiar.FamiliarId);
                _npcBuilder
                    .Create()
                    .WithId(familiar.FamiliarId)
                    .WithLocation(Character.Location)
                    .WithScript(scriptType, script =>
                    {
                        var familiarScript = (IFamiliarScript)script;
                        if (familiarScript is IHydratable<HydratedFamiliar> hydratable)
                        {
                            hydratable.Hydrate(new HydratedFamiliar
                            {
                                TicksRemaining = familiar.TicksRemaining,
                                IsUsingSpecialMove = familiar.IsUsingSpecialMove,
                                SpecialMovePoints = familiar.SpecialMovePoints
                            });
                        }

                        if (inventory is not null && familiarScript is IHydratable<IReadOnlyList<HydratedItem>> hydratableInventory)
                        {
                            hydratableInventory.Hydrate(inventory);
                        }

                        familiarScript.AttachToSummoner(Character, definition);
                        Character.AttachFamiliar(familiarScript);
                    })
                    .Spawn();
            }
            finally
            {
                _familiar = null;
                _inventory = null;
            }
        }
    }
}
