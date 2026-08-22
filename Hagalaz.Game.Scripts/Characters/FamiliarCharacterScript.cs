using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Characters
{
    public class FamiliarCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        public FamiliarCharacterScript(ICharacterContextAccessor contextAccessor)
            : base(contextAccessor) { }

        protected override void Initialize() { }

        public override void OnRegistered()
        {
            // Owner-aware familiar restoration remains on the existing pre-NPC
            // composition path and is outside this dependency refactor.
        }
    }
}
