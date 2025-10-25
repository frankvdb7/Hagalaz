using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Defaults;
using NSubstitute;
using Xunit;

namespace Hagalaz.Cache.Tests.Types.Defaults;

public class EquipmentDefaultsTests
{
    [Fact]
    public void Read_ShouldParseAllOpcodesCorrectly()
    {
        // Arrange
        var cacheApi = Substitute.For<ICacheAPI>();
        var stream = new MemoryStream();

        // Write test data for all opcodes
        // Opcode 1: BodySlotData
        var bodySlotData = new byte[] { 10, 20, 30 };
        stream.WriteByte(1);
        stream.WriteByte((byte)bodySlotData.Length);
        stream.Write(bodySlotData, 0, bodySlotData.Length);

        // Opcode 3: OffHandSlot
        byte offHandSlot = 123;
        stream.WriteByte(3);
        stream.WriteByte(offHandSlot);

        // Opcode 4: MainHandSlot
        byte mainHandSlot = 45;
        stream.WriteByte(4);
        stream.WriteByte(mainHandSlot);

        // Opcode 5: ShieldData
        var shieldData = new byte[] { 1, 2, 3, 4 };
        stream.WriteByte(5);
        stream.WriteByte((byte)shieldData.Length);
        stream.Write(shieldData, 0, shieldData.Length);

        // Opcode 6: WeaponData
        var weaponData = new byte[] { 5, 6 };
        stream.WriteByte(6);
        stream.WriteByte((byte)weaponData.Length);
        stream.Write(weaponData, 0, weaponData.Length);

        // Opcode 0: End of data
        stream.WriteByte(0);
        stream.Position = 0;

        var container = new Container(stream);
        cacheApi.ReadContainer(28, 6).Returns(container);

        // Act
        var defaults = EquipmentDefaults.Read(cacheApi);

        // Assert
        Assert.NotNull(defaults);
        Assert.Equal(bodySlotData, defaults.BodySlotData);
        Assert.Equal(offHandSlot, defaults.OffHandSlot);
        Assert.Equal(mainHandSlot, defaults.MainHandSlot);
        Assert.Equal(shieldData.Length, defaults.ShieldData.Length);
        for(int i = 0; i < shieldData.Length; i++)
        {
            Assert.Equal(shieldData[i], defaults.ShieldData[i]);
        }
        Assert.Equal(weaponData.Length, defaults.WeaponData.Length);
        for(int i = 0; i < weaponData.Length; i++)
        {
            Assert.Equal(weaponData[i], defaults.WeaponData[i]);
        }
    }
}
