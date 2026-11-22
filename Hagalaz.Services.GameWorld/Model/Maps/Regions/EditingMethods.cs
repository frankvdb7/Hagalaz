using System;
using System.Collections.Generic;
using System.Linq;
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

            var parts = new List<int>(PartsPerRegionAxis * PartsPerRegionAxis * Planes);
            for (var localPartX = 0; localPartX < PartsPerRegionAxis; localPartX++)
            {
                for (var localPartY = 0; localPartY < PartsPerRegionAxis; localPartY++)
                {
                    for (var z = 0; z < Planes; z++)
                    {
                        parts.Add(GetPartKey(localPartX, localPartY, z));
                        var part = (MapRegionPart)GetRegionPartByLocalPart(localPartX, localPartY, z);
                        part.DrawRegionPartX = LocalPartXToPartX(localPartX);
                        part.DrawRegionPartY = LocalPartYToPartY(localPartY);
                        part.DrawRegionZ = z;
                        part.HasDrawSource = true;
                        part.Rotation = 0;
                    }
                }
            }

            IsDynamic = false;
            RebuildParts(parts, parts, new Dictionary<int, MapRegionTemplate> { [Id] = standardTemplate }, _parts.ToList());
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
        /// Finds the part at a local part index of this region without creating it.
        /// </summary>
        public IMapRegionPart? FindRegionPartByLocalPart(int localPartX, int localPartY, int z) =>
            _parts.GetOrDefault(LocationHelper.GetRegionPartHash(LocalPartXToPartX(localPartX), LocalPartYToPartY(localPartY), z));

        /// <summary>
        /// Maps a local part of this dynamic region to a source part and rebuilds the affected
        /// content from the source templates.
        /// </summary>
        public void WriteBlockByIndex(int localPartX, int localPartY, int z, int sourcePartX, int sourcePartY, int sourceZ, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            EnsureAcceptsMutation();
            EnsureDynamic("Writing");
            ValidateEdit(localPartX, localPartY, z, new PartDescriptor(sourcePartX, sourcePartY, sourceZ, true, 0));
            var current = PartDescriptor.Of(GetRegionPartByLocalPart(localPartX, localPartY, z));
            EditParts([(localPartX, localPartY, z, new PartDescriptor(sourcePartX, sourcePartY, sourceZ, true, current.Rotation))], templates);
        }

        /// <summary>
        /// Maps several local parts of this dynamic region at once and rebuilds each affected
        /// column a single time.
        /// </summary>
        public void WriteBlocksByIndex(IReadOnlyCollection<MapRegionPartMapping> mappings, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            EnsureAcceptsMutation();
            EnsureDynamic("Writing");
            ArgumentNullException.ThrowIfNull(mappings);
            EditParts(mappings
                .Select(mapping => (mapping.LocalPartX, mapping.LocalPartY, mapping.Z,
                    new PartDescriptor(mapping.SourcePartX, mapping.SourcePartY, mapping.SourceZ, true, mapping.Rotation)))
                .ToList(), templates);
        }

        /// <summary>
        /// Rotates a local part of this dynamic region and rebuilds the affected content from the source templates.
        /// </summary>
        public void RotateBlockByIndex(int localPartX, int localPartY, int z, int rotation, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            EnsureAcceptsMutation();
            EnsureDynamic("Rotating");
            ValidateEdit(localPartX, localPartY, z, PartDescriptor.Absent with { Rotation = rotation });
            var current = PartDescriptor.Of(GetRegionPartByLocalPart(localPartX, localPartY, z));
            if (current.Rotation == rotation)
            {
                return;
            }

            EditParts([(localPartX, localPartY, z, current with { Rotation = rotation })], templates);
        }

        /// <summary>
        /// Marks a local part absent and rebuilds the affected content from the source templates.
        /// </summary>
        public void DeleteBlockByIndex(int localPartX, int localPartY, int z, IReadOnlyDictionary<int, MapRegionTemplate> templates)
        {
            EnsureAcceptsMutation();
            EnsureDynamic("Deleting");
            ValidateEdit(localPartX, localPartY, z, PartDescriptor.Absent);
            EditParts([(localPartX, localPartY, z, PartDescriptor.Absent)], templates);
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
