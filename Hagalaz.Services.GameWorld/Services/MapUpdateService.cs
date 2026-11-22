using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Protocol;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// Orchestrates synchronous map updates at the character render boundary.
    /// </summary>
    public sealed class MapUpdateService : IMapUpdateService
    {
        private const int PartsPerRegionAxis = 8;
        private const int Planes = 4;

        private readonly IMapRegionService _mapRegionService;

        public MapUpdateService(IMapRegionService mapRegionService)
        {
            _mapRegionService = mapRegionService;
        }

        public void UpdateMap(ICharacter character, bool forceUpdate, bool renderViewPort = false)
        {
            var viewport = character.Viewport;
            if (viewport.VisibleRegions.Count == 0 || viewport.ShouldRebuild())
            {
                viewport.RebuildView();
            }

            viewport.RefreshVisibleRegions();
            var visibleRegions = viewport.VisibleRegions;

            if (viewport.NeedsDynamicDraw())
            {
                var (chunks, sourceRegionXteaKeys) = BuildDynamicChunks(viewport);
                character.Session.SendMessage(new DrawDynamicMapMessage
                {
                    MapSizeIndex = viewport.MapSize.Type,
                    RenderViewport = renderViewPort,
                    ForceUpdate = forceUpdate,
                    CharacterIndex = character.Index,
                    CharacterLocation = character.Location,
                    RegionPartX = viewport.ViewLocation.RegionPartX,
                    RegionPartY = viewport.ViewLocation.RegionPartY,
                    Chunks = chunks,
                    SourceRegionXteaKeys = sourceRegionXteaKeys
                });
            }
            else
            {
                character.Session.SendMessage(new DrawStandardMapMessage
                {
                    MapSizeIndex = viewport.MapSize.Type,
                    RenderViewport = renderViewPort,
                    ForceUpdate = forceUpdate,
                    CharacterIndex = character.Index,
                    CharacterLocation = character.Location,
                    RegionPartX = viewport.ViewLocation.RegionPartX,
                    RegionPartY = viewport.ViewLocation.RegionPartY,
                    VisibleRegionXteaKeys = visibleRegions.Select(region => region.XteaKeys).ToList()
                });
            }

            viewport.BeginMapRegionSynchronization();
        }

        /// <summary>
        /// Reads the viewport's chunk slots in client order (plane, part X, part Y) and collects
        /// the XTEA keys of each distinct referenced source region in first-seen order.
        /// </summary>
        private (IReadOnlyList<DynamicMapChunk> Chunks, IReadOnlyList<int[]> SourceRegionXteaKeys) BuildDynamicChunks(IViewport viewport)
        {
            var partsPerAxis = viewport.MapSize.Size >> 3;
            var basePartX = viewport.ViewLocation.RegionPartX - (viewport.MapSize.Size >> 4);
            var basePartY = viewport.ViewLocation.RegionPartY - (viewport.MapSize.Size >> 4);
            var regions = viewport.VisibleRegions.ToDictionary(region => region.Id);
            var chunks = new List<DynamicMapChunk>(Planes * partsPerAxis * partsPerAxis);
            var sourceRegionIds = new HashSet<int>();
            var sourceRegionXteaKeys = new List<int[]>();

            for (var z = 0; z < Planes; z++)
            {
                for (var viewPartX = 0; viewPartX < partsPerAxis; viewPartX++)
                {
                    for (var viewPartY = 0; viewPartY < partsPerAxis; viewPartY++)
                    {
                        var partX = basePartX + viewPartX;
                        var partY = basePartY + viewPartY;
                        if (partX < 0 || partY < 0
                            || !regions.TryGetValue(((partX / PartsPerRegionAxis) << 8) | (partY / PartsPerRegionAxis), out var region))
                        {
                            chunks.Add(DynamicMapChunk.Absent);
                            continue;
                        }

                        var part = region.GetRegionPartByLocalPart(partX % PartsPerRegionAxis, partY % PartsPerRegionAxis, z);
                        if (!part.HasDrawSource)
                        {
                            chunks.Add(DynamicMapChunk.Absent);
                            continue;
                        }

                        chunks.Add(DynamicMapChunk.Present(part.DrawRegionPartX, part.DrawRegionPartY, part.DrawRegionZ, part.Rotation));
                        var sourceRegionId = ((part.DrawRegionPartX / PartsPerRegionAxis) << 8) | (part.DrawRegionPartY / PartsPerRegionAxis);
                        if (sourceRegionIds.Add(sourceRegionId))
                        {
                            sourceRegionXteaKeys.Add(_mapRegionService.GetXtea(sourceRegionId));
                        }
                    }
                }
            }

            return (chunks, sourceRegionXteaKeys);
        }
    }
}
