using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Hagalaz.Cache.Abstractions.Providers;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class MapRotationHelperTests
    {
        [DataTestMethod]
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
            var objectTypeProviderMock = new Mock<ITypeProvider<IObjectType>>();
            var objectTypeMock = new Mock<IObjectType>();
            objectTypeMock.SetupGet(o => o.SizeX).Returns(sizeX);
            objectTypeMock.SetupGet(o => o.SizeY).Returns(sizeY);
            objectTypeProviderMock.Setup(p => p.Get(It.IsAny<int>())).Returns(objectTypeMock.Object);

            // Act
            var result = MapRotationHelper.CalculateObjectPartRotation(objectTypeProviderMock.Object, 1, objectRotation, xIndex, yIndex, partRotation, calculateRotationY);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
