using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests.Types
{
    public class EquipmentDefaultsTests
    {
        [Fact]
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
