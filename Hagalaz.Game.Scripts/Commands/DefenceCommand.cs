using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Scripts.Commands
{
    public class DefenceCommand : IGameCommand
    {
        public string Name => "defence";
        public Permission Permission => Permission.GameAdministrator;

        public Task Execute(GameCommandArgs args)
        {
            args.Character.Statistics.Bonuses.SetBonus(BonusType.DefenceStab, 2000);
            args.Character.Statistics.Bonuses.SetBonus(BonusType.DefenceRanged, 2000);
            args.Character.Statistics.Bonuses.SetBonus(BonusType.DefenceMagic, 2000);
            args.Character.Statistics.Bonuses.SetBonus(BonusType.DefenceCrush, 2000);
            args.Character.Statistics.Bonuses.SetBonus(BonusType.DefenceSummoning, 2000);
            args.Character.Statistics.Bonuses.SetBonus(BonusType.DefenceSlash, 2000);
            args.Character.SendChatMessage("Be carefull with that...");
            return Task.CompletedTask;
        }
    }
}
