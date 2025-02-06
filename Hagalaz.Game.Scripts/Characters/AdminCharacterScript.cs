using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Characters
{
    public class AdminCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        public AdminCharacterScript(ICharacterContextAccessor contextAccessor) : base(contextAccessor)
        {
        }

        protected override void Initialize() { }

        public override bool CanBeLootedBy(ICreature killer)
        {
            // prevent losing items by admin or higher
            if (Character.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
            {
                return false;
            }
            if (killer is ICharacter killerCharacter)
            {
                if (killerCharacter.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
