using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common;
using Hagalaz.Game.Messages.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Contains methods for managing character regions.
    /// </summary>
    public partial class Character : Creature
    {
        /// <summary>
        /// Update's character visible regions.
        /// </summary>
        public async Task UpdateMapAsync(bool forceUpdate, bool renderViewPort = false)
        {
            Viewport.RebuildView();
            if (Viewport.NeedsDynamicDraw())
            {
                // TODO dynamic map
                Session.SendMessage(new DrawDynamicMapMessage());
                //await Session.SendPacketAsync(await ConstructMapDynamicPacketComposer.WriteDataAsync(new ConstructMapDynamicPacketComposer(), this, true));
            }
            else
            {
                Session.SendMessage(new DrawStandardMapMessage
                {
                    MapSizeIndex = Viewport.MapSize.Type,
                    RenderViewport = renderViewPort,
                    ForceUpdate = forceUpdate,
                    CharacterIndex = Index,
                    CharacterLocation = Location,
                    RegionPartX = Viewport.ViewLocation.RegionPartX,
                    RegionPartY = Viewport.ViewLocation.RegionPartY,
                    VisibleRegionXteaKeys = Viewport.VisibleRegions.Select(region => region.XteaKeys).ToList()
                });
            }
            await Viewport.UpdateViewport();
        }

        /// <summary>
        /// Notifies character that it must add itself to given region.
        /// </summary>
        /// <param name="newRegion"></param>
        protected override void AddToRegion(IMapRegion newRegion) => newRegion.Add(this);

        /// <summary>
        /// Notifies character that it must remove itself from given region.
        /// </summary>
        /// <param name="region"></param>
        protected override void RemoveFromRegion(IMapRegion region) => region.Remove(this);

        /// <summary>
        /// Notifier for region relink.
        /// </summary>
        protected override void OnRegionChange()
        {
            this.QueueTask(async () =>
            {
                var musicService = ServiceProvider.GetRequiredService<IMusicService>();
                var musicIds = await musicService.FindMusicIdsByRegionId(Region.Id);
                if (musicIds.Any(musicId => Music.UnlockMusic(musicId)))
                {
                    Music.RefreshMusicList();
                }

                if (musicIds.Any())
                {
                    Music.PlayMusic(musicIds[RandomStatic.Generator.Next(0, musicIds.Count)]);
                }
            });
        }
    }
}