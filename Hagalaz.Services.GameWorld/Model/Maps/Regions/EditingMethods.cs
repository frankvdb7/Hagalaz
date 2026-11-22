using System;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions
{
    /// <summary>
    /// Contains methods for editing map region.
    /// </summary>
    public partial class MapRegion
    {
        private const int PartsPerRegionAxis = 8;
        private const int Planes = 4;

        /// <summary>
        /// Maps every part to itself, present and unrotated, and rebuilds all
        /// objects and floor collision from the region's own template.
        /// </summary>
        /// <param name="standardTemplate">The template of this region.</param>
        public void MakeStandard(MapRegionTemplate standardTemplate)
        {
            EnsureAcceptsMutation();
            EnsureTemplate(standardTemplate, Id);
            for (var z = 0; z < Planes; z++)
            {
                for (var localPartX = 0; localPartX < PartsPerRegionAxis; localPartX++)
                {
                    for (var localPartY = 0; localPartY < PartsPerRegionAxis; localPartY++)
                    {
                        DiscardPartContent(GetRegionPartByLocalPart(localPartX, localPartY, z), localPartX, localPartY, z);
                    }
                }
            }

            for (var z = 0; z < Planes; z++)
            {
                for (var localPartX = 0; localPartX < PartsPerRegionAxis; localPartX++)
                {
                    for (var localPartY = 0; localPartY < PartsPerRegionAxis; localPartY++)
                    {
                        var part = GetRegionPartByLocalPart(localPartX, localPartY, z);
                        part.DrawRegionPartX = LocalPartXToPartX(localPartX);
                        part.DrawRegionPartY = LocalPartYToPartY(localPartY);
                        part.DrawRegionZ = z;
                        part.HasDrawSource = true;
                        part.Rotation = 0;
                        MaterializePart(part, localPartX, localPartY, z, standardTemplate);
                    }
                }
            }

            IsDynamic = false;
        }

        /// <summary>
        /// Make's this region dynamic.
        /// </summary>
        public void MakeDynamic()
        {
            EnsureAcceptsMutation();
            IsDynamic = true;
        }

        public int LocalPartXToPartX(int index) => BaseLocation.RegionX * PartsPerRegionAxis + index;
        public int LocalPartYToPartY(int index) => BaseLocation.RegionY * PartsPerRegionAxis + index;
        public int LocalXToPart(int localX) => LocalPartXToPartX(localX / 8);
        public int LocalYToPart(int localY) => LocalPartYToPartY(localY / 8);

        /// <summary>
        /// Gets the part at absolute part coordinates (absolute tile / 8).
        /// </summary>
        public IMapRegionPart GetRegionPartByAbsolutePart(int partX, int partY, int z)
        {
            EnsureAcceptsMutation();
            var partHash = LocationHelper.GetRegionPartHash(partX, partY, z);
            return _parts.GetOrAdd(partHash, CreateRegionPart);
        }

        /// <summary>
        /// Gets the part at a local part index of this region.
        /// </summary>
        /// <param name="localPartX">0-7.</param>
        /// <param name="localPartY">0-7.</param>
        /// <param name="z">0-3.</param>
        public IMapRegionPart GetRegionPartByLocalPart(int localPartX, int localPartY, int z) =>
            GetRegionPartByAbsolutePart(LocalPartXToPartX(localPartX), LocalPartYToPartY(localPartY), z);

        /// <summary>
        /// Maps a local part of this dynamic region to a source part and rebuilds it from the source template.
        /// </summary>
        public void WriteBlockByIndex(int localPartX, int localPartY, int z, int sourcePartX, int sourcePartY, int sourceZ, MapRegionTemplate sourceTemplate)
        {
            EnsureAcceptsMutation();
            EnsureDynamic("Writing");
            EnsureTemplate(sourceTemplate, GetSourceRegionId(sourcePartX, sourcePartY));
            var part = GetRegionPartByLocalPart(localPartX, localPartY, z);
            DiscardPartContent(part, localPartX, localPartY, z);
            part.DrawRegionPartX = sourcePartX;
            part.DrawRegionPartY = sourcePartY;
            part.DrawRegionZ = sourceZ;
            part.HasDrawSource = true;
            MaterializePart(part, localPartX, localPartY, z, sourceTemplate);
        }

        /// <summary>
        /// Rotates a local part of this dynamic region and rebuilds it from its source template.
        /// </summary>
        public void RotateBlockByIndex(int localPartX, int localPartY, int z, int rotation, MapRegionTemplate sourceTemplate)
        {
            EnsureAcceptsMutation();
            EnsureDynamic("Rotating");
            var part = GetRegionPartByLocalPart(localPartX, localPartY, z);
            if (part.Rotation == rotation)
            {
                return;
            }

            if (part.HasDrawSource)
            {
                EnsureTemplate(sourceTemplate, GetSourceRegionId(part.DrawRegionPartX, part.DrawRegionPartY));
            }

            DiscardPartContent(part, localPartX, localPartY, z);
            part.Rotation = rotation;
            MaterializePart(part, localPartX, localPartY, z, sourceTemplate);
        }

        /// <summary>
        /// Removes a local part's content and marks it absent.
        /// </summary>
        public void DeleteBlockByIndex(int localPartX, int localPartY, int z)
        {
            EnsureAcceptsMutation();
            EnsureDynamic("Deleting");
            var part = GetRegionPartByLocalPart(localPartX, localPartY, z);
            DiscardPartContent(part, localPartX, localPartY, z);
            part.Erase();
        }

        internal static int GetSourceRegionId(int sourcePartX, int sourcePartY) =>
            ((sourcePartX / PartsPerRegionAxis) << 8) | (sourcePartY / PartsPerRegionAxis);

        private void EnsureDynamic(string operation)
        {
            if (!IsDynamic)
            {
                throw new NotSupportedException($"{operation} blocks is not supported in non-dynamic regions!");
            }
        }

        private static void EnsureTemplate(MapRegionTemplate template, int expectedRegionId)
        {
            ArgumentNullException.ThrowIfNull(template);
            if (template.RegionId != expectedRegionId)
            {
                throw new ArgumentException($"Template of region {template.RegionId} does not match source region {expectedRegionId}.", nameof(template));
            }
        }
    }
}
