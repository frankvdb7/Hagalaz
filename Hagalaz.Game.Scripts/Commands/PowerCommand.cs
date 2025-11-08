using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Scripts.Commands
{
    public class PowerCommand : IGameCommand
    {
        public string Name => "power";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            args.Character.Statistics.Bonuses.SetBonus(BonusType.Strength, 2000);
            args.Character.Statistics.Bonuses.SetBonus(BonusType.RangedStrength, 2000);
            args.Character.Statistics.Bonuses.SetBonus(BonusType.MagicDamage, 2000);
            args.Character.SendChatMessage("Be carefull with that...");
            return Task.CompletedTask;
        }
    }
}
