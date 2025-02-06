using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Orbs
{
    /// <summary>
    ///     Represents hp orb.
    /// </summary>
    public class ConstitutionOrb : WidgetScript
    {
        public ConstitutionOrb(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen() => Refresh();

        /// <summary>
        ///     Refreshes this instance.
        /// </summary>
        public void Refresh()
        {
            Owner.Statistics.RefreshLifePoints();
            Owner.Statistics.RefreshPoison();
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }
    }
}