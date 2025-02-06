using System;
using System.Text;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    /// </summary>
    public class QuestTab : WidgetScript
    {
        public QuestTab(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) {}

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.DrawString(10, "<col=00BFFF>Information Tab");
            var builder = new StringBuilder();
            builder.Append("              <col=00BFFF>General");
            builder.Append("<br><br>");
            var rights = Owner.Permissions.ToClientRights();
            builder.Append("Rank:");
            builder.Append("<br>");
            builder.Append((rights > 0 ? "<img=" + (rights <= 3 ? rights - 1 : rights) + ">" : string.Empty) + " " + Owner.Permissions.ToRightsTitle());
            builder.Append("<br><br>");
            builder.Append("            <col=00BFFF>PvP Statistics");
            builder.Append("<br><br>");
            builder.Append("PvP Kills: ");
            var kills = Owner.Profile.GetValue<int>(ProfileConstants.PvpKillCount);
            builder.Append(kills);
            builder.Append("<br>");
            builder.Append("PvP Deaths: ");
            var deaths = Owner.Profile.GetValue<int>(ProfileConstants.PvpDeathCount);
            builder.Append(deaths);
            builder.Append("<br>");
            var killDeathRatio = 1.0;
            if (deaths == 0)
            {
                killDeathRatio = kills == 0 ? 1.0 : kills;
            }
            else if (kills == 0)
            {
                killDeathRatio = 0.0;
            }
            else
            {
                killDeathRatio = kills / (double)deaths;
            }

            builder.Append("PvP K/D Ratio: ");
            builder.Append(Math.Round(killDeathRatio, 2, MidpointRounding.AwayFromZero));

            InterfaceInstance.DrawString(16, builder.ToString());
        }
    }
}