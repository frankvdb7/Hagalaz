using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    /// <summary>
    /// Contains nonstandard movement rendering definition.
    /// </summary>
    public class ForceMovement : IForceMovement
    {
        /// <summary>
        /// Location where movement starts.
        /// </summary>
        public required ILocation StartLocation { get; init; }

        /// <summary>
        /// Location where movement ends.
        /// </summary>
        public required ILocation EndLocation { get; init; }

        /// <summary>
        /// Speed per which the character will move to start location 
        /// from current location. 1 = 30ms, 2 = 60ms and so on.
        /// </summary>
        public int EndSpeed { get; init; }

        /// <summary>
        /// Speed per which the character will move to end location
        /// from current location. 1 = 30ms, 2 = 60ms and so on.
        /// </summary>
        public int StartSpeed { get; init; }

        /// <summary>
        /// Contains character facing direction while moving.
        /// </summary>
        public FaceDirection FaceDirection { get; init; }
        
        /// <summary>
        /// Calculates the face direction.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="tileSizeX">The tile size x.</param>
        /// <param name="tileSizeY">The tile size y.</param>
        /// <returns></returns>
        public static int CalculateFaceDirection(ICreature creature, ILocation start, ILocation end, int tileSizeX = 1, int tileSizeY = 1)
        {
            int targetMapX = -1;
            int targetMapY = -1;
            creature.Viewport.GetLocalPosition(end, ref targetMapX, ref targetMapY);
            int targetX = targetMapX * 2 + tileSizeX;
            int targetY = targetMapY * 2 + tileSizeY;
            int renderableMapX = -1;
            int renderableMapY = -1;
            creature.Viewport.GetLocalPosition(start, ref renderableMapX, ref renderableMapY);
            int tileX = renderableMapX * 512 + creature.Size * 256;
            int tileY = renderableMapY * 512 + creature.Size * 256;
            int targetTileX = targetX * 256;
            int targetTileY = targetY * 256;
            int deltaX = tileX - targetTileX;
            int deltaY = tileY - targetTileY;
            return CalculateFaceDirection(deltaX, deltaY);
        }

        /// <summary>
        /// Calculates the face direction.
        /// </summary>
        /// <param name="deltaX">The delta x.</param>
        /// <param name="deltaY">The delta y.</param>
        /// <returns></returns>
        private static int CalculateFaceDirection(int deltaX, int deltaY)
        {
            if (deltaX != 0 || deltaY != 0)
                return (int)(Math.Atan2(deltaX, deltaY) * 2607.5945876176133) & 0x3fff;
            return 0;
        }
    }
}