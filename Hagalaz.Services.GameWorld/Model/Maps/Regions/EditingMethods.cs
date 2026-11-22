using System;
using System.Collections.Generic;
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
            ArgumentNullException.ThrowIfNull(standardTemplate);
            if (standardTemplate.RegionId != Id)
            {
                throw new ArgumentException($"Template of region {standardTemplate.RegionId} is not the template of region {Id}.", nameof(standardTemplate));
            }

            var columns = new List<int>(PartsPerRegionAxis * PartsPerRegionAxis);
            for (var localPartX = 0; localPartX < PartsPerRegionAxis; localPartX++)
            {
                for (var localPartY = 0; localPartY < PartsPerRegionAxis; localPartY++)
                {
                    columns.Add(GetColumnKey(localPartX, localPartY));
                    for (var z = 0; z < Planes; z++)
                    {
                        var part = GetRegionPartByLocalPart(localPartX, localPartY, z);
                        part.DrawRegionPartX = LocalPartXToPartX(localPartX);
                        part.DrawRegionPartY = LocalPartYToPartY(localPartY);
                        part.DrawRegionZ = z;
                        part.HasDrawSource = true;
                        part.Rotation = 0;
                    }
                }
            }

            IsDynamic = false;
            RebuildColumns(columns, new Dictionary<int, MapRegionTemplate> { [Id] = standardTemplate });
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
        /// Maps a local part of this dynamic region to a source part and rebuilds the affected
        /// content from the source templates.
        /// </summary>
        public void WriteBlockByIndex(int localPartX, int localPartY, int z, int sourcePartX, int sourcePartY, int sourceZ, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            EnsureAcceptsMutation();
            EnsureDynamic("Writing");
            EditPart(localPartX, localPartY, z, templates, part =>
            {
                part.DrawRegionPartX = sourcePartX;
                part.DrawRegionPartY = sourcePartY;
                part.DrawRegionZ = sourceZ;
                part.HasDrawSource = true;
            });
        }

        /// <summary>
        /// Rotates a local part of this dynamic region and rebuilds the affected content from the source templates.
        /// </summary>
        public void RotateBlockByIndex(int localPartX, int localPartY, int z, int rotation, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            EnsureAcceptsMutation();
            EnsureDynamic("Rotating");
            if (GetRegionPartByLocalPart(localPartX, localPartY, z).Rotation == rotation)
            {
                return;
            }

            EditPart(localPartX, localPartY, z, templates, part => part.Rotation = rotation);
        }

        /// <summary>
        /// Marks a local part absent and rebuilds the affected content from the source templates.
        /// </summary>
        public void DeleteBlockByIndex(int localPartX, int localPartY, int z, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            EnsureAcceptsMutation();
            EnsureDynamic("Deleting");
            EditPart(localPartX, localPartY, z, templates, part => part.Erase());
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
    }
}
