using Hagalaz.Cache.Types.Defaults;
using Hagalaz.Cache.Types.Defaults.Codecs;
using Xunit;

namespace Hagalaz.Cache.Tests.Types.Defaults.Codecs
{
    public class EquipmentDefaultsCodecTests
    {
        [Fact]
        public void RoundTrip_ShouldPreserveAllProperties()
        {
            // Arrange
            var codec = new EquipmentDefaultsCodec();
            var original = new EquipmentDefaults
            {
                BodySlotData = new byte[] { 10, 20, 30 },
                OffHandSlot = 123,
                MainHandSlot = 45,
                ShieldData = new[] { 1, 2, 3, 4 },
                WeaponData = new[] { 5, 6 }
            };

            // Act
            var encoded = codec.Encode(original);
            var decoded = codec.Decode(0, encoded);

            // Assert
            Assert.Equal(original.BodySlotData, decoded.BodySlotData);
            Assert.Equal(original.OffHandSlot, decoded.OffHandSlot);
            Assert.Equal(original.MainHandSlot, decoded.MainHandSlot);
            Assert.Equal(original.ShieldData, decoded.ShieldData);
            Assert.Equal(original.WeaponData, decoded.WeaponData);
        }
    }
}
