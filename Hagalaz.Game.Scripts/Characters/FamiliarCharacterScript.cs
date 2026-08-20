using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Characters
{
    public class FamiliarCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        private readonly INpcBuilder _npcBuilder;
        private readonly ISummoningService _summoningService;

        public FamiliarCharacterScript(ICharacterContextAccessor contextAccessor, INpcBuilder npcBuilder, ISummoningService summoningService)
            : base(contextAccessor) =>
            (_npcBuilder, _summoningService) = (npcBuilder, summoningService);

        protected override void Initialize() { }

        public override void OnRegistered()
        {
            if (!Character.HasFamiliar())
            {
                return;
            }

            var definition = _summoningService.FindDefinitionByNpcId(Character.FamiliarId).GetAwaiter().GetResult();
            if (definition is null)
            {
                return;
            }

            _npcBuilder
                .Create()
                .WithId(Character.FamiliarId)
                .WithLocation(Character.Location)
                .WithScript((activator, owner) => Character.CreateFamiliar(owner, definition, activator))
                .Spawn();
        }
    }
}
