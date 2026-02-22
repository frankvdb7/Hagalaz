using NSubstitute;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class MapRotationHelperTests
    {
        [TestMethod]
        [DataRow(1, 1, 0, 0, 0, 0, false, 0)]
        [DataRow(1, 1, 0, 0, 0, 0, true, 0)]
        [DataRow(2, 1, 0, 1, 0, 1, false, 0)]
        [DataRow(2, 1, 0, 1, 0, 1, true, 0)]
        [DataRow(2, 1, 0, 1, 0, 2, false, 0)]
        [DataRow(2, 1, 0, 1, 0, 2, true, 0)]
        [DataRow(2, 1, 0, 1, 0, 3, false, 0)]
        [DataRow(2, 1, 0, 1, 0, 3, true, 1)]
        public void CalculateObjectPartRotation_RotatesCorrectly(int sizeX, int sizeY, int objectRotation, int xIndex, int yIndex, int partRotation, bool calculateRotationY, int expected)
        {
            // Arrange
            var objectTypeProviderMock = Substitute.For<ITypeProvider<IObjectType>>();
            var objectTypeMock = Substitute.For<IObjectType>();
            objectTypeMock.SizeX.Returns(sizeX);
            objectTypeMock.SizeY.Returns(sizeY);
            objectTypeProviderMock.Get(Arg.Any<int>()).Returns(objectTypeMock);

            // Act
            var result = MapRotationHelper.CalculateObjectPartRotation(objectTypeProviderMock, 1, objectRotation, xIndex, yIndex, partRotation, calculateRotationY);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
