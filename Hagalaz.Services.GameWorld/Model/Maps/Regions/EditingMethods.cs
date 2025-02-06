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
        /// <summary>
        /// Make's region standard, and removes any changes
        /// made to it.
        /// </summary>
        public void MakeStandard()
        {
            for (var z = 0; z < 4; z++)
            {
                for (var x = 0; x < 8; x++)
                {
                    for (var y = 0; y < 8; y++)
                    {
                        var partX = BaseLocation.RegionX * 8 + x;
                        var partY = BaseLocation.RegionY * 8 + y;
                        var partHash = LocationHelper.GetRegionPartHash(partX, partY, z);
                        if (_parts.TryGetValue(partHash, out var part))
                        {
                            part.DrawRegionPartX = partX;
                            part.DrawRegionPartY = partY;
                            part.DrawRegionZ = z;
                            part.Rotation = 0;
                        }
                    }
                }
            }

            IsDynamic = false;
        }

        /// <summary>
        /// Make's this region dynamic.
        /// </summary>
        public void MakeDynamic() => IsDynamic = true;

        public static int PartToLocalPart(int part) => part - part / 8 * 8;
        public int LocalPartXToPartX(int index) => BaseLocation.RegionX * 8 + index;
        public int LocalPartYToPartY(int index) => BaseLocation.RegionY * 8 + index;
        public int LocalXToPart(int localX) => LocalPartXToPartX(localX / 8);
        public int LocalYToPart(int localY) => LocalPartYToPartY(localY / 8);

        public IMapRegionPart GetRegionPartData(int partX, int partY, int z)
        {
            var partHash = LocationHelper.GetRegionPartHash(partX, partY, z);
            return _parts.GetOrAdd(partHash, CreateRegionPart);
        }

        /// <summary>
        /// Get's region part data of given part.
        /// </summary>
        /// <param name="localPartX">0-7.</param>
        /// <param name="localPartY">0-7.</param>
        /// <param name="z">0-3.</param>
        /// <returns>RegionPartData.</returns>
        public IMapRegionPart GetRegionPartByLocalPart(int localPartX, int localPartY, int z)
        {
            var partX = LocalPartXToPartX(localPartX);
            var partY = LocalPartYToPartY(localPartY);
            var partHash = LocationHelper.GetRegionPartHash(partX, partY, z);
            return _parts.GetOrAdd(partHash, CreateRegionPart);
        }

        /// <summary>
        /// Writes region data to given block.
        /// </summary>
        /// <param name="partX">PartX of region to write. PartX = AbsX / 8.</param>
        /// <param name="partY">PartY of region to write. PartY = AbsY / 8.</param>
        /// <param name="z">The z.</param>
        /// <param name="drawPartX">The draw part X.</param>
        /// <param name="drawPartY">The draw part Y.</param>
        /// <param name="drawPartZ">The draw part Z.</param>
        public void WriteBlock(int partX, int partY, int z, int drawPartX, int drawPartY, int drawPartZ) => WriteBlockByIndex(PartToLocalPart(partX), PartToLocalPart(partY), z, drawPartX, drawPartY, drawPartZ);

        public void WriteBlockByIndex(int localPartX, int localPartY, int z, int drawPartX, int drawPartY, int drawPartZ)
        {
            if (!IsDynamic)
            {
                throw new NotSupportedException("Writing blocks is not supported in non-dynamic regions!");
            }
            var partX = LocalPartXToPartX(localPartX);
            var partY = LocalPartYToPartY(localPartY);
            var partHash = LocationHelper.GetRegionPartHash(partX, partY, z);
            var part = _parts.GetOrAdd(partHash, CreateRegionPart);
            part.DrawRegionPartX = drawPartX;
            part.DrawRegionPartY = drawPartY;
            part.DrawRegionZ = drawPartZ;
            UnloadPartGameObjects(partX, partY, z, part.Rotation);
            LoadPartObjects(partX, partY, z, part.Rotation);
        }

        public void RotateBlock(int partX, int partY, int z, int rotation) => RotateBlockByIndex(PartToLocalPart(partX), PartToLocalPart(partY), z, rotation);

        public void RotateBlockByIndex(int localPartX, int localPartY, int z, int rotation)
        {
            if (!IsDynamic)
            {
                throw new NotSupportedException("Rotating blocks is not supported in non-dynamic regions!");
            }
            var partX = LocalPartXToPartX(localPartX);
            var partY = LocalPartXToPartX(localPartY);
            var partHash = LocationHelper.GetRegionPartHash(partX, partY, z);
            var part = _parts.GetOrAdd(partHash, CreateRegionPart);
            if (part.Rotation != rotation)
            {
                UnloadPartGameObjects(partX, partY, z, part.Rotation);
                part.Rotation = rotation;
                LoadPartObjects(partX, partY, z, rotation);
            }
        }

        public void DeleteBlock(int partX, int partY, byte z) => DeleteBlockByIndex(PartToLocalPart(partX), PartToLocalPart(partY), z);

        public void DeleteBlockByIndex(int localPartX, int localPartY, int z)
        {
            if (!IsDynamic)
            {
                throw new NotSupportedException("Deleting blocks is not supported in non-dynamic regions!");
            }
            var partX = LocalPartXToPartX(localPartX);
            var partY = LocalPartXToPartX(localPartY);
            var partHash = LocationHelper.GetRegionPartHash(partX, partY, z);
            var part = _parts.GetOrAdd(partHash, CreateRegionPart);
            UnloadPartGameObjects(partX, partY, z, part.Rotation);
            part.Erase();
        }
    }
}