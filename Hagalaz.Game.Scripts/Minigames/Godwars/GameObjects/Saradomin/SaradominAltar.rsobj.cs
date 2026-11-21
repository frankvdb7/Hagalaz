using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Saradomin
{
    [GameObjectScriptMetaData([26287])]
    public class SaradominAltar : GodwarsAltarScript<SaradominAltarPrayedState>
    {
        protected override (int X, int Y) TeleportDestination => (2923, 5257);
    }
}
