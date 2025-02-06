using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Characters;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets
{
    /// <summary>
    ///     Contains world map script.
    /// </summary>
    public class WorldMap : WidgetScript
    {
        //private EventManager.EventHappened setMarkerHandler;

        public WorldMap(ICharacterContextAccessor contextAccessor) : base(contextAccessor)
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            Owner.QueueAnimation(Animation.Create(840));

            /*this.setMarkerHandler = this.owner.RegisterEventHandler<WorldMapSetMarkerEvent>(new EventManager.EventHappened((e) =>
            {
                int wayPointHash = ((WorldMapSetMarkerEvent)e).LocationHash;
                short wx = (short)(wayPointHash >> 14);
                short wy = (short)(wayPointHash); // & 0x3fff
                byte wz = (byte)(wayPointHash >> 28);
                Location loc = Location.Create(wx, wy, wz, 0);
                if (this.owner.Preferences.WorldMapMarker == null || !this.owner.Preferences.WorldMapMarker.TargetLocation.Equals(loc))
                {
                    if (this.owner.HasHintIcon(this.owner.Preferences.WorldMapMarker))
                    {
                        this.owner.Preferences.WorldMapMarker.TargetLocation = loc;
                        this.owner.Preferences.WorldMapMarker.RenderFor(this.owner);
                    }
                    else
                    {
                        HintIcon wp = HintIcon.CreateLocationHintIcon(loc, 2, 20, 2);
                        if (this.owner.RegisterHintIcon(wp) != null)
                            this.owner.Preferences.WorldMapMarker = wp;
                    }
                    this.owner.Configurations.SendStandartConfiguration(1159, wayPointHash);
                }
                return true; // event handled
            }));*/

            //this.interfaceInstance.SetOptions(1, 0, 19, 2);

            //int z = this.owner.Location.Z << 28;
            //(Y & 0x3fff) | ((X & 0x3fff) << 14) | ((Z & 0x3) << 28)
            var locationHash = (Owner.Location.Y & 0x3fff) | ((Owner.Location.X & 0x3fff) << 14) | ((Owner.Location.Z & 0x3) << 28);

            // For some reason the surface map will not load correctly.

            Owner.Configurations.SendGlobalCs2Int(622, locationHash); // center the map to character location

            Owner.Configurations.SendGlobalCs2Int(674, locationHash); // character location

            InterfaceInstance.AttachClickHandler(46, (componentID, type, extraInfo1, extraInfo2) =>
            {
                if (type == ComponentClickType.LeftClick)
                {
                    Owner.UpdateMapAsync(true).Wait();
                    Owner.GetScript<WidgetsCharacterScript>()?.OpenMainGameFrame();
                    return true;
                }

                return false;
            });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() => Owner.QueueAnimation(Animation.Create(-1));
    }
}