using Hagalaz.Cache.Types;


namespace Hagalaz.Cache.Tests.Types
{
    [TestClass]
    public class EquipmentDefaultsTests
    {
        [TestMethod]
        public void Constructor_ShouldInitializeArrays()
        {
            // Arrange & Act
            var defaults = new EquipmentDefaults();

            // Assert
            Assert.NotNull(defaults.BodySlotData);
            Assert.NotNull(defaults.WeaponData);
            Assert.NotNull(defaults.ShieldData);
            Assert.Empty(defaults.BodySlotData);
            Assert.Empty(defaults.WeaponData);
            Assert.Empty(defaults.ShieldData);
        }
    }
}
