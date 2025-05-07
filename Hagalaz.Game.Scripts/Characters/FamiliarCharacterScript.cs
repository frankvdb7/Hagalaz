using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Characters
{
    public class FamiliarCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        private readonly INpcBuilder _npcBuilder;

        public FamiliarCharacterScript(ICharacterContextAccessor contextAccessor, INpcBuilder npcBuilder)
            : base(contextAccessor) =>
            _npcBuilder = npcBuilder;

        protected override void Initialize() { }

        public override void OnRegistered()
        {
            if (!Character.HasFamiliar())
            {
                return;
            }

            _npcBuilder
                .Create()
                .WithId(Character.FamiliarScript.Familiar.Appearance.CompositeID)
                .WithLocation(Character.Location)
                .WithScript(Character.FamiliarScript)
                .Spawn();
        }
    }
}