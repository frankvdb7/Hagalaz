using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Services.GameWorld
{
    public static class MapRotationHelper
    {
        private const int ROTATION_AXIS_SWAP_FLAG = 0x1;

        public static int CalculateObjectPartRotation(ITypeProvider<IObjectType> objectTypeProvider, int objectId, int objectRotation, int xIndex, int yIndex, int partRotation, bool calculateRotationY)
        {
            var objectType = objectTypeProvider.Get(objectId);
            if (objectType == null)
            {
                return calculateRotationY ? yIndex : xIndex;
            }

            int sizeX = (objectRotation & ROTATION_AXIS_SWAP_FLAG) != 1 ? objectType.SizeX : objectType.SizeY;
            int sizeY = (objectRotation & ROTATION_AXIS_SWAP_FLAG) != 1 ? objectType.SizeY : objectType.SizeX;

            if (partRotation == 1)
            {
                return calculateRotationY ? sizeX - 1 - xIndex : yIndex;
            }
            else if (partRotation == 2)
            {
                return calculateRotationY ? sizeY - 1 - yIndex : sizeX - 1 - xIndex;
            }
            else if (partRotation == 3)
            {
                return calculateRotationY ? xIndex : sizeY - 1 - yIndex;
            }
            else
            {
                return calculateRotationY ? yIndex : xIndex;
            }
        }
    }
}
