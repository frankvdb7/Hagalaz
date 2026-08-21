using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Factories;

namespace Hagalaz.Game.Scripts.Characters
{
    public class FamiliarCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        private readonly IFamiliarFactory _familiarFactory;

        public FamiliarCharacterScript(
            ICharacterContextAccessor contextAccessor,
            IFamiliarFactory familiarFactory)
            : base(contextAccessor) =>
            _familiarFactory = familiarFactory;

        protected override void Initialize() { }

        public override void OnRegistered()
        {
            _familiarFactory.TryRestore(Character);
        }
    }
}
