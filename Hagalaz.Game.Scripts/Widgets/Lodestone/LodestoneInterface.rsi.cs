using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Widgets.Lodestone
{
    /// <summary>
    /// </summary>
    public class LodestoneInterface : WidgetScript
    {
        private readonly ILodestoneService _lodestoneService;

        public LodestoneInterface(ICharacterContextAccessor characterContextAccessor, ILodestoneService lodestoneService) : base(characterContextAccessor)
        {
            _lodestoneService = lodestoneService;
        }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
            foreach (var def in _lodestoneService.FindAll().GetAwaiter().GetResult())
            {
                InterfaceInstance.AttachClickHandler(def.ComponentId, (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType == ComponentClickType.LeftClick)
                    {
                        if (!Owner.HasState(def.State))
                        {
                            Owner.SendChatMessage("You must activate this lodestone to teleport to it.");
                            return false;
                        }

                        new HomeTeleport(Location.Create(def.CoordX, def.CoordY, def.CoordZ)).PerformTeleport(Owner);
                        return true;
                    }

                    return false;
                });
            }
        }

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
        }
    }
}