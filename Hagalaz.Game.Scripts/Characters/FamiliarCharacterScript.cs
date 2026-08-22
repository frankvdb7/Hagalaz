using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Characters
{
    public class FamiliarCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        private readonly INpcBuilder _npcBuilder;
        private readonly IFamiliarScriptProvider _familiarScriptProvider;
        private readonly ISummoningService _summoningService;

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

        public override void OnRegistered()
        {
            var familiarId = Character.PendingFamiliarId;
            if (familiarId == 0)
            {
                return;
            }

            var definition = _summoningService.FindDefinitionByNpcIdSync(familiarId);
            if (definition is null)
            {
                Character.ClearPendingFamiliar();
                return;
            }

            var scriptType = _familiarScriptProvider.FindFamiliarScriptTypeById(familiarId);
            _npcBuilder
                .Create()
                .WithId(familiarId)
                .WithLocation(Character.Location)
                .WithScript((activator, owner) =>
                {
                    var script = (IFamiliarScript)activator.Create(scriptType, owner);
                    Character.ApplyPendingFamiliar(script);
                    script.AttachToSummoner(Character, definition);
                    Character.AttachFamiliar(script);
                    return script;
                })
                .Spawn();
        }
    }
}
