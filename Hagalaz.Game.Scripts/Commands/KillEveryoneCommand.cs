using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Collections.Extensions;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Commands
{
    public class KillEveryoneCommand : IGameCommand
    {
        public string Name => "killeveryone";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            args.Character.Speak("TROLLSTRIKE!!!");
            args.Character.Viewport.VisibleCreatures.OfType<INpc>()
                .ForEach(npc =>
                {
                    args.Character.Combat.PerformAttack(new AttackParams()
                    {
                        Damage = 1337, MaxDamage = 1337, DamageType = DamageType.StandardMagic, Target = npc
                    });
                });
            return Task.CompletedTask;
        }
    }
}
